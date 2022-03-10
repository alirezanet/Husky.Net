using System.IO.Abstractions;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class ExecCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IFileSystem _io;
      private readonly IGit _git;

      public ExecCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _io = Substitute.For<IFileSystem>();
         _git = Substitute.For<IGit>();
      }

      [Fact]
      public async Task Exec_WithoutCache_WhenFileMissing_ThrowException()
      {
         // Arrange
         const string filePath = "fake_file.csx";
         var command = new ExecCommand(_io, _git) { Path = filePath, NoCache = true };

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
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git) { Path = filePath, NoCache = true };

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
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git) { Path = filePath, NoCache = true };

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
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git) { Path = filePath, NoCache = true };

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
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git) { Path = filePath, NoCache = true };

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
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io, _git) { Path = filePath, Arguments = new List<string> { "test" }, NoCache = true };

         // Act
         await command.ExecuteAsync(_console);
      }
   }
}
