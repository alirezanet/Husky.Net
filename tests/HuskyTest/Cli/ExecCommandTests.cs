using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class ExecCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IFileSystem _io;
      private readonly IGit _git;
      private readonly IAssembly _assembly;

      public ExecCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _io = Substitute.For<IFileSystem>();
         _git = Substitute.For<IGit>();
         _assembly = Substitute.For<IAssembly>();
      }

      [Fact]
      public async Task Exec_WithoutCache_WhenFileMissing_ThrowException()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, NoCache = true };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage($"can not find script file on '{filePath}'");
      }

      [Fact]
      public async Task Exec_WithoutCache_WithErrorInScript_ThrowException()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = "BadCode";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, NoCache = true };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>()
            .Where(e => e.Message.EndsWith("script compilation failed"));
      }

      [Fact]
      public async Task Exec_WithoutCache_WithScriptThrowException_ThrowException()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = @"
               throw new Exception(""Inner script exception."");
            ";
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, NoCache = true };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage("Inner script exception.");
      }

      [Fact]
      public async Task Exec_WithoutCache_WithScriptReturnGreaterThan0_ThrowException()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = @"
               return 1;
            ";
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, NoCache = true };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage("script execution failed");
      }

      [Fact]
      public async Task Exec_WithoutCache_WithoutArguments_Succeed()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = @"
               Console.WriteLine(""Test"");
            ";
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, NoCache = true };

         // Act
         await command.ExecuteAsync(_console);
      }

      [Fact]
      public async Task Exec_WithoutCache_WithArguments_Succeed()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = @"
               Console.WriteLine(Args[0]);
            ";
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, Arguments = new List<string> { "test" }, NoCache = true };

         // Act
         await command.ExecuteAsync(_console);
      }

      [Fact]
      public async Task Exec_CachedScript_ShouldHandleNonZeroExitCode()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         const string stringContent = @"
               return 1;
            ";
         _io.Path.GetDirectoryName(Arg.Any<string>()).Returns(Directory.GetCurrentDirectory());
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.Directory.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var stream = new MemoryStream(Encoding.UTF8.GetBytes(stringContent));
         _io.FileStream.Create(Arg.Any<string>(), FileMode.Open).Returns(stream);

         var opts = ScriptOptions.Default
            .WithSourceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, null))
            .WithImports("System", "System.IO", "System.Collections.Generic", "System.Text", "System.Threading.Tasks");
         var script = CSharpScript.Create(stringContent.Trim(), opts, typeof(Globals));
         var compilation = script.GetCompilation();

         await using var assemblyStream = new MemoryStream();
         var result = compilation.Emit(assemblyStream);
         _assembly.LoadFile(Arg.Any<string>()).Returns(Assembly.Load(assemblyStream.ToArray()));

         var command = new ExecCommand(_io, _git, _assembly) { Path = filePath, Arguments = new List<string> { "test" }, NoCache = false };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("script execution failed");
      }
   }
}
