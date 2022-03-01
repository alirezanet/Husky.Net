using System.IO.Abstractions;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class ExecCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IFileSystem _io;

      public ExecCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _io = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task Exec_WhenFileMissing_ThrowException()
      {
         // Arrange
         string filePath = "fake_file.csx";
         var command = new ExecCommand(_io) { Path = filePath };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage($"can not find script file on '{filePath}'");
      }

      [Fact]
      public async Task Exec_WithErrorInScript_ThrowException()
      {
         // Arrange
         string filePath = "fake_file.csx";
         string stringContent = "BadCode";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io) { Path = filePath };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage("The name 'BadCode' does not exist in the current context\nscript compilation failed");
      }

      [Fact]
      public async Task Exec_WithScriptThrowException_ThrowException()
      {
         // Arrange
         string filePath = "fake_file.csx";
         string stringContent = @"
               throw new Exception(""Inner script exception."");
            ";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io) { Path = filePath };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage("Inner script exception.");
      }

      [Fact]
      public async Task Exec_WithScriptReturnGreaterThan0_ThrowException()
      {
         // Arrange
         string filePath = "fake_file.csx";
         string stringContent = @"
               return 1;
            ";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io) { Path = filePath };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage("script execution failed");
      }

      [Fact]
      public async Task Exec_WithoutArguments_Succeed()
      {
         // Arrange
         string filePath = "fake_file.csx";
         string stringContent = @"
               Console.WriteLine(""Test"");
            ";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io) { Path = filePath };

         // Act
         await command.ExecuteAsync(_console);
      }

      [Fact]
      public async Task Exec_WithArguments_Succeed()
      {
         // Arrange
         string filePath = "fake_file.csx";
         string stringContent = @"
               Console.WriteLine(Args[0]);
            ";
         _io.File.Exists(Arg.Any<string>()).Returns(true);
         _io.File.ReadAllTextAsync(Arg.Any<string>()).Returns(stringContent);
         var command = new ExecCommand(_io) { Path = filePath, Arguments = new List<string> { "test" } };

         // Act
         await command.ExecuteAsync(_console);
      }
   }
}
