using System.IO.Abstractions;
using System.Xml.Linq;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Cli.AttachServices;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli;

public class AttachCommandTests
{
   private readonly FakeInMemoryConsole _console;

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
      _io.FileInfo.FromFileName(Arg.Any<string>()).Returns(fileInfoWrapper);
      _io.Directory.GetCurrentDirectory().Returns(_currentDirectory);
      const string _csprojXml =
         "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.1</TargetFramework></PropertyGroup></Project>";
      _xmlDoc = XElement.Parse(_csprojXml);
      _xmlIo = Substitute.For<IXmlIO>();
      _xmlIo.Load(Arg.Any<string>()).Returns(_xmlDoc);
   }

   [Fact]
   public async Task Attach_WhenParametersProvided_ShouldAddHuskyTargetElement()
   {
      // Arrange
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlIo.Received(1).Load(Arg.Any<string>());
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Is(_xmlDoc));
      _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "Husky")?.Descendants("Exec")
         .Should().NotBeNull().And.HaveCount(2);
      _console.ReadOutputString().Trim().Should().Be("Husky dev-dependency successfully attached to this project.");
   }

   [Fact]
   public async Task Attach_WhenHuskyTargetAlreadyExists_ShouldReturnEarly()
   {
      // Arrange
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

   [Theory]
   [InlineData("/f1/f2", "/f1/f2", "project.csproj", new[] { "." })]
   [InlineData("/f1/f2/f3", "/f1/f2", "../project.csproj", new[] { "." })]
   [InlineData("/f1/f2", "/f1/f2", "f3/project.csproj", new[] { ".." })]
   [InlineData("/f1/f2", "/f1/f2", "f3/f4/project.csproj", new[] { "..", ".." })]
   [InlineData("/f1/f2/f3/f4", "/f1/f2", "project.csproj", new[] { "..", ".." })]
   public async Task Attach_WorkingDirectoryShouldBeRelativePathToProjectRoot(string currentDirectory, string projectPath, string fileName, string[] relativePath)
   {
      // Arrange
      _git.GetGitPathAsync().Returns(projectPath);
      _currentDirectory = currentDirectory;
      _fileName = fileName;
      var fileInfoWrapper = new FileInfoWrapper(_io, new FileInfo(Path.Combine(_currentDirectory, _fileName)));
      _io.FileInfo.FromFileName(Arg.Any<string>()).Returns(fileInfoWrapper);
      var command = new AttachCommand(_git, _io, _xmlIo) { FileName = _fileName };

      // Act
      await command.ExecuteAsync(_console);

      // Assert
      _xmlIo.Received(1).Save(Arg.Any<string>(), Arg.Any<XElement>());

      var exec = _xmlDoc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value == "Husky")?
         .Descendants("Exec").FirstOrDefault(q => q.Attribute("Command")?.Value == "dotnet husky install");

      exec.Should().NotBeNull();
      exec!.Attribute("WorkingDirectory")?.Value.Should().Be(string.Join(Path.DirectorySeparatorChar, relativePath));
   }
}
