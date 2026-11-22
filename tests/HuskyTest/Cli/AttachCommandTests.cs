using System.IO.Abstractions;
using System.Xml.Linq;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli;

public class AttachCommandTests
{
   private FakeInMemoryConsole _console;

   private string _currentDirectory;
   private string _fileName;
   private readonly IGit _git;
   private readonly IFileSystem _io;
   private readonly XElement _xmlDoc;
   private readonly IXmlIO _xmlIo;

   public AttachCommandTests()
   {
      _console = new FakeInMemoryConsole();
      LoggerEx.logger = new Logger(_console);
      _currentDirectory = "/f1/f2";
      _fileName = "/f1/f2/project.csproj";
      _git = Substitute.For<IGit>();
      _git.GetGitPathAsync().Returns(_currentDirectory); // usually same as current directory but can be different
      _io = Substitute.For<IFileSystem>();
      var fileInfoWrapper = new FileInfoWrapper(_io, new FileInfo(Path.Combine(_currentDirectory, _fileName)));
      _io.FileInfo.New(Arg.Any<string>()).Returns(fileInfoWrapper);
      _io.Directory.GetCurrentDirectory().Returns(_currentDirectory);
      const string csprojXml =
         "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.1</TargetFramework></PropertyGroup></Project>";
      _xmlDoc = XElement.Parse(csprojXml);
      _xmlIo = Substitute.For<IXmlIO>();
      _xmlIo.Load(Arg.Any<string>()).Returns(_xmlDoc);
   }

   [Fact]
   public async Task Attach_WhenParametersProvided_ShouldAddHuskyTargetElement()
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      LoggerEx.logger = new Logger(_console);
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlIo.Received(1).Load(Arg.Any<string>());
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Is(_xmlDoc));

      // Verify HuskyRoot PropertyGroup
      var huskyRoot = _xmlDoc.Descendants("PropertyGroup")
         .SelectMany(pg => pg.Descendants("HuskyRoot"))
         .FirstOrDefault();
      huskyRoot.Should().NotBeNull();
      huskyRoot!.Attribute("Condition")?.Value.Should().Be("'$(HuskyRoot)' == ''");

      var huskyTarget = _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "Husky");
      huskyTarget.Should().NotBeNull();
      huskyTarget!.Descendants("Exec").Should().HaveCount(2);
      huskyTarget.Attribute("Condition")?.Value.Should().Contain("!Exists(");
      huskyTarget.Attribute("Inputs").Should().BeNull();
      huskyTarget.Attribute("Outputs").Should().BeNull();

      // Verify Touch is conditioned on directory existence
      huskyTarget.Descendants("Touch").Should().HaveCount(1);
      var touch = huskyTarget.Descendants("Touch").First();
      touch.Attribute("AlwaysCreate")?.Value.Should().Be("true");
      touch.Attribute("Condition")?.Value.Should().Contain("Exists(");

      huskyTarget.Descendants("ItemGroup").Descendants("FileWrites").Should().BeEmpty();

      // A sibling HuskyClean target deletes the stamp on Clean so the next build re-installs
      var huskyCleanTarget = _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "HuskyClean");
      huskyCleanTarget.Should().NotBeNull();
      huskyCleanTarget!.Attribute("AfterTargets")?.Value.Should().Be("Clean");
      var delete = huskyCleanTarget.Descendants("Delete").Should().ContainSingle().Subject;
      delete.Attribute("Files")?.Value.Should().Be("$(HuskyRoot).husky/_/install.stamp");

      _console.ReadOutputString().Trim().Should().Be("Husky dev-dependency successfully attached to this project.");
   }

   [Fact]
   public async Task Attach_WhenHuskyTargetAlreadyExists_ShouldReturnEarly()
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      LoggerEx.logger = new Logger(_console);
      _xmlDoc.Add(new XElement("Target", new XAttribute("Name", "Husky")));
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _console.ReadOutputString().Trim().Should().Be("Husky is already attached to this project.");
      _xmlIo.Received(0).Save(Arg.Any<string>(), Arg.Any<XElement>());
   }

   [Fact]
   public async Task Attach_WhenForceIsTrue_ShouldReplaceTargetTag()
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      _xmlDoc.Add(new XElement("Target", new XAttribute("Name", "Husky")));
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName, Force = true };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlDoc.Descendants("Target").Should().HaveCount(2);
      _xmlDoc.Descendants("Target")
         .SingleOrDefault(q => q.Attribute("Name")?.Value == "Husky")?.Descendants("Exec")
         .Should().NotBeNull().And.HaveCount(2);
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());
   }

   [Fact]
   public async Task Attach_WhenForceIsTrue_ShouldPreserveOtherPropertiesInPropertyGroup()
   {
      // Arrange - HuskyRoot shares a PropertyGroup with another property
      _console = new FakeInMemoryConsole();
      _xmlDoc.Add(new XElement("Target", new XAttribute("Name", "Husky")));
      var sharedPg = new XElement("PropertyGroup",
         new XElement("SomeOtherProperty", "value"),
         new XElement("HuskyRoot", "../../"));
      _xmlDoc.Add(sharedPg);
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName, Force = true };

      // Act
      await command.ExecuteAsync(_console);

      // Assert - SomeOtherProperty should survive, old HuskyRoot removed, new one added
      _xmlDoc.Descendants("SomeOtherProperty").Should().HaveCount(1);
      _xmlDoc.Descendants("HuskyRoot").Should().HaveCount(1);
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());
   }

   [Fact]
   public async Task Attach_WhenIgnoreSubmoduleIsTrue_ShouldAddSubmoduleTargetCondition()
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      _xmlDoc.Add(new XElement("Target", new XAttribute("Name", "Husky")));
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName, Force = true, IgnoreSubmodule = true };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlDoc.Descendants("Target").Should().HaveCount(2);
      _xmlDoc.Descendants("Target")
         .SingleOrDefault(q => q.Attribute("Name")?.Value == "Husky")?.Descendants("Exec")
         .Should().NotBeNull().And.HaveCount(2);
      _xmlDoc.Descendants("Target")
         .SingleOrDefault(q => q.Attribute("Name")?.Value == "Husky")
         ?.Attribute("Condition")
         ?.Value.Should().Contain(" and '$(IgnoreSubmodule)' != 0");
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());
   }

   [Fact]
   public async Task Attach_WhenIgnoreSubmoduleIsTrue_ShouldAddInstallSubModuleParameter()
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      _xmlDoc.Add(new XElement("Target", new XAttribute("Name", "Husky")));
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName, Force = true, IgnoreSubmodule = true };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlDoc.Descendants("Target").Should().HaveCount(2);

      var targetExecElements = _xmlDoc.Descendants("Target")
         .SingleOrDefault(q => q.Attribute("Name")?.Value == "Husky")?.Descendants("Exec").ToList();

      targetExecElements.Should().NotBeNull().And.HaveCount(2);
      targetExecElements!.SingleOrDefault(e => e.Attribute("Command")!.Value.Contains("dotnet husky install --ignore-submodule")).Should().NotBeNull();
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());
   }

   [Theory]
   [InlineData("/f1/f2", "/f1/f2", "project.csproj", new[] { "." })]
   [InlineData("/f1/f2/f3", "/f1/f2", "../project.csproj", new[] { "." })]
   [InlineData("/f1/f2", "/f1/f2", "f3/project.csproj", new[] { ".." })]
   [InlineData("/f1/f2", "/f1/f2", "f3/f4/project.csproj", new[] { "..", ".." })]
   [InlineData("/f1/f2/f3/f4", "/f1/f2", "project.csproj", new[] { "..", ".." })]
   public async Task Attach_WorkingDirectoryShouldBeRelativePathToProjectRoot(string currentDirectory, string projectPath, string fileName, string[] relativePath)
   {
      // Arrange
      _console = new FakeInMemoryConsole();
      _git.GetGitPathAsync().Returns(projectPath);
      _currentDirectory = currentDirectory;
      _fileName = fileName;
      var fileInfoWrapper = new FileInfoWrapper(_io, new FileInfo(Path.Combine(_currentDirectory, _fileName)));
      _io.FileInfo.New(Arg.Any<string>()).Returns(fileInfoWrapper);
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());

      var expectedHuskyRoot = string.Join("/", relativePath) + "/";
      var expected = XElement.Parse(
         $$"""
         <Project Sdk="Microsoft.NET.Sdk">
           <PropertyGroup><TargetFramework>netcoreapp2.1</TargetFramework></PropertyGroup>
           <PropertyGroup><HuskyRoot Condition="'$(HuskyRoot)' == ''">{{expectedHuskyRoot}}</HuskyRoot></PropertyGroup>
           <Target Name="Husky" AfterTargets="Restore" Condition="'$(HUSKY)' != 0 and !Exists('$(HuskyRoot).husky/_/install.stamp')">
             <Exec Command="dotnet tool restore" StandardOutputImportance="Low" StandardErrorImportance="High" />
             <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High" WorkingDirectory="$(HuskyRoot)" />
             <Touch Files="$(HuskyRoot).husky/_/install.stamp" AlwaysCreate="true" Condition="Exists('$(HuskyRoot).husky/_')" />
           </Target>
           <Target Name="HuskyClean" AfterTargets="Clean">
             <Delete Files="$(HuskyRoot).husky/_/install.stamp" />
           </Target>
         </Project>
         """);

      _xmlDoc.ToString().Should().Be(expected.ToString());
   }
}
