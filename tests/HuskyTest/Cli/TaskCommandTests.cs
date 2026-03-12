using System.IO.Abstractions;
using System.Text.Json;
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
   public class TaskListCommandTests
   {
      private const string GitPath = "/repo";
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public TaskListCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetGitPathAsync().Returns(GitPath);
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task TaskList_WhenTaskRunnerJsonNotFound_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         var command = new TaskListCommand(_git, _fileSystem);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("task-runner.json not found, try 'husky install'");
      }

      [Fact]
      public async Task TaskList_WhenNoTasksExist_Succeeds()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(@"{""tasks"": []}");
         var command = new TaskListCommand(_git, _fileSystem);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().NotThrowAsync();
      }

      [Fact]
      public async Task TaskList_WhenTasksExist_ReadsTaskRunnerJson()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         var json = JsonSerializer.Serialize(new
         {
            tasks = new[] { new { name = "my-task", command = "dotnet", group = "pre-commit" } }
         });
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(json);
         var command = new TaskListCommand(_git, _fileSystem);

         // Act
         await command.ExecuteAsync(console);

         // Assert
         await _fileSystem.File.Received(1).ReadAllTextAsync(Arg.Any<string>());
      }
   }

   public class TaskAddCommandTests
   {
      private const string GitPath = "/repo";
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public TaskAddCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetGitPathAsync().Returns(GitPath);
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task TaskAdd_WhenTaskRunnerJsonNotFound_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
         var command = new TaskAddCommand(_git, _fileSystem) { Name = "my-task", Command = "dotnet" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("task-runner.json not found, try 'husky install'");
      }

      [Fact]
      public async Task TaskAdd_WhenTaskAlreadyExists_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         var existingJson = JsonSerializer.Serialize(new { tasks = new[] { new { name = "my-task", command = "dotnet" } } });
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(existingJson);
         var command = new TaskAddCommand(_git, _fileSystem) { Name = "my-task", Command = "npm" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("*my-task*already exists*");
      }

      [Fact]
      public async Task TaskAdd_WithValidTask_WritesUpdatedJson()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(@"{""tasks"": []}");
         var command = new TaskAddCommand(_git, _fileSystem)
         {
            Name = "my-task",
            Command = "dotnet",
            Args = ["test"],
            Group = "pre-commit"
         };

         // Act
         await command.ExecuteAsync(console);

         // Assert
         await _fileSystem.File.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("my-task") && s.Contains("dotnet") && s.Contains("pre-commit")));
      }
   }

   public class TaskUpdateCommandTests
   {
      private const string GitPath = "/repo";
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public TaskUpdateCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetGitPathAsync().Returns(GitPath);
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task TaskUpdate_WhenTaskNotFound_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(@"{""tasks"": []}");
         var command = new TaskUpdateCommand(_git, _fileSystem) { Name = "nonexistent" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("*nonexistent*not found*");
      }

      [Fact]
      public async Task TaskUpdate_WhenTaskExists_WritesUpdatedJson()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         var existingJson = JsonSerializer.Serialize(new { tasks = new[] { new { name = "my-task", command = "dotnet" } } });
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(existingJson);
         var command = new TaskUpdateCommand(_git, _fileSystem) { Name = "my-task", Command = "npm" };

         // Act
         await command.ExecuteAsync(console);

         // Assert
         await _fileSystem.File.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("npm") && s.Contains("my-task")));
      }
   }

   public class TaskRemoveCommandTests
   {
      private const string GitPath = "/repo";
      private const string HuskyPath = ".husky";

      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly IFileSystem _fileSystem;

      public TaskRemoveCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         _git = Substitute.For<IGit>();
         _git.GetGitPathAsync().Returns(GitPath);
         _git.GetHuskyPathAsync().Returns(HuskyPath);

         _fileSystem = Substitute.For<IFileSystem>();
      }

      [Fact]
      public async Task TaskRemove_WhenTaskNotFound_ThrowException()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(@"{""tasks"": []}");
         var command = new TaskRemoveCommand(_git, _fileSystem) { Name = "nonexistent" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("*nonexistent*not found*");
      }

      [Fact]
      public async Task TaskRemove_WhenTaskExists_WritesJsonWithoutTask()
      {
         // Arrange
         var console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(console);
         var existingJson = JsonSerializer.Serialize(new { tasks = new[] { new { name = "my-task", command = "dotnet" } } });
         _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
         _fileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(existingJson);
         var command = new TaskRemoveCommand(_git, _fileSystem) { Name = "my-task" };

         // Act
         await command.ExecuteAsync(console);

         // Assert
         await _fileSystem.File.Received(1).WriteAllTextAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => !s.Contains("my-task")));
      }
   }
}
