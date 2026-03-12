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
   public class HookListCommandTests
   {
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public HookListCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task HookList_WhenHuskyNotInstalled_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
         var command = new HookListCommand(_git, _fileSystem);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>()
            .WithMessage("can not find husky required files (try: husky install)");
      }

      [Fact]
      public async Task HookList_WhenNoHooksExist_Succeeds()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.Directory.GetFiles(HuskyPath).Returns([]);
         var command = new HookListCommand(_git, _fileSystem);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().NotThrowAsync();
      }

      [Fact]
      public async Task HookList_WhenHooksExist_ReadsDirectory()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.Directory.GetFiles(HuskyPath).Returns([
            Path.Combine(HuskyPath, "pre-commit"),
            Path.Combine(HuskyPath, "commit-msg"),
            Path.Combine(HuskyPath, "task-runner.json")
         ]);
         _fileSystem.Path.GetFileName(Path.Combine(HuskyPath, "pre-commit")).Returns("pre-commit");
         _fileSystem.Path.GetFileName(Path.Combine(HuskyPath, "commit-msg")).Returns("commit-msg");
         _fileSystem.Path.GetFileName(Path.Combine(HuskyPath, "task-runner.json")).Returns("task-runner.json");
         var command = new HookListCommand(_git, _fileSystem);

         // Act
         await command.ExecuteAsync(console);

         // Assert
         _fileSystem.Directory.Received(1).GetFiles(HuskyPath);
      }
   }

   public class HookRemoveCommandTests
   {
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public HookRemoveCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task HookRemove_WhenHuskyNotInstalled_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
         var command = new HookRemoveCommand(_git, _fileSystem) { HookName = "pre-commit" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>()
            .WithMessage("can not find husky required files (try: husky install)");
      }

      [Fact]
      public async Task HookRemove_WhenHookNameContainsPathSeparator_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(true);
         var command = new HookRemoveCommand(_git, _fileSystem) { HookName = $"pre-commit{Path.DirectorySeparatorChar}commit" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("hook name can not contain path separator");
      }

      [Fact]
      public async Task HookRemove_WhenHookNotFound_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(true);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "pre-commit")).Returns(false);
         var command = new HookRemoveCommand(_git, _fileSystem) { HookName = "pre-commit" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("*pre-commit*not found*");
      }

      [Fact]
      public async Task HookRemove_WhenHookExists_DeletesHook()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(true);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "pre-commit")).Returns(true);
         var command = new HookRemoveCommand(_git, _fileSystem) { HookName = "pre-commit" };

         // Act
         await command.ExecuteAsync(console);

         // Assert
         _fileSystem.File.Received(1).Delete(Path.Combine(HuskyPath, "pre-commit"));
      }
   }
}
