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

      var huskyTarget = _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "Husky");
      huskyTarget.Should().NotBeNull();
      huskyTarget!.Descendants("Exec").Should().HaveCount(2);
      huskyTarget.Attribute("Inputs").Should().NotBeNull();
      huskyTarget.Attribute("Outputs").Should().NotBeNull();
      huskyTarget.Descendants("Touch").Should().HaveCount(1);
      huskyTarget.Descendants("Touch").First().Attribute("AlwaysCreate")?.Value.Should().Be("true");
      huskyTarget.Descendants("ItemGroup").Descendants("FileWrites").Should().HaveCount(1);

      _console.ReadOutputString().Trim().Should().Be("Husky dev-dependency successfully attached to this project.");

      huskyTarget.Attribute("AfterTargets")?.Value.Should().Be("Restore");
      huskyTarget.Attribute("BeforeTargets").Should().BeNull();
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
      _xmlDoc.Descendants("Target").Should().HaveCount(1);
      _xmlDoc.Descendants("Target")
         .SingleOrDefault(q => q.Attribute("Name")?.Value == "Husky")?.Descendants("Exec")
         .Should().NotBeNull().And.HaveCount(2);
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
      _xmlDoc.Descendants("Target").Should().HaveCount(1);
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
      _xmlDoc.Descendants("Target").Should().HaveCount(1);

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

      var huskyTarget = _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "Husky");
      huskyTarget.Should().NotBeNull();

      var rootRelativePath = string.Join(Path.DirectorySeparatorChar, relativePath);

      var exec = huskyTarget!.Descendants("Exec")
         .FirstOrDefault(q => q.Attribute("Command")?.Value == "dotnet husky install");
      exec.Should().NotBeNull();
      exec!.Attribute("WorkingDirectory")?.Value.Should().Be(rootRelativePath);

      var expectedSentinel = Path.Combine(rootRelativePath, ".husky", "_", "install.stamp");
      var expectedInput = Path.Combine(rootRelativePath, ".config", "dotnet-tools.json");
      huskyTarget.Attribute("Inputs")?.Value.Should().Be(expectedInput);
      huskyTarget.Attribute("Outputs")?.Value.Should().Be(expectedSentinel);
      huskyTarget.Descendants("Touch").First().Attribute("Files")?.Value.Should().Be(expectedSentinel);
      huskyTarget.Descendants("ItemGroup").Descendants("FileWrites").First().Attribute("Include")?.Value.Should().Be(expectedSentinel);
   }
}
