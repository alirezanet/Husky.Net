using System.IO.Abstractions;
using System.Runtime.Intrinsics.X86;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class InstallCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly ICliWrap _cliWrap;
      private readonly IFileSystem _fileSystem;

      public InstallCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _fileSystem = Substitute.For<IFileSystem>();
         _git = Substitute.For<IGit>();
         _cliWrap = Substitute.For<ICliWrap>();
      }

      [Fact]
      public async Task Install_WhenGitIsNotFound_ThrowException()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(-1, now, now)));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("Git hooks installation failed");
      }

      [Fact]
      public async Task Install_WhenTheTopFolderIsNotAGitRepository_ThrowException()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("rev-parse --git-dir").Returns(new BufferedCommandResult(0, now, now, "C:\\TestPath\\.git\\branch", ""));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>()
            .WithMessage(".git can't be found (see https://alirezanet.github.io/Husky.Net/guide/getting-started)\nGit hooks installation failed");
      }

      [Fact]
      public async Task Install_WhenTheTopFolderIsNotAGitRepositoryButItIsAWorktree_NotThrowException()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(false);
         _git.GetGitDirectory(Arg.Any<string>()).Returns(@"C:\TestPath\.git\worktrees\branch", "");
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("config --local --list").Returns(new BufferedCommandResult(0, now, now, "gitflow", ""));
         _git.ExecAsync("config gitflow.path.hooks .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().NotThrowAsync();
      }

      [Fact]
      public async Task Install_WhenTheGitConfigurationFailed_ThrowException()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(-1, now, now)));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("Failed to configure git\nGit hooks installation failed");
      }

      [Fact]
      public async Task Install_WhenTheGitflowConfigurationFailed_ThrowException()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("config --local --list").Returns(new BufferedCommandResult(0, now, now, "gitflow", ""));
         _git.ExecAsync("config gitflow.path.hooks .husky").Returns(Task.FromResult(new CommandResult(-1, now, now)));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("Failed to configure gitflow\nGit hooks installation failed");
      }

      [Fact]
      public async Task Install_Succeed()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("config --local --list").Returns(new BufferedCommandResult(0, now, now, "gitflow", ""));
         _git.ExecAsync("config gitflow.path.hooks .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));

         // Act
         await command.ExecuteAsync(_console);
      }

      [Fact(Skip = "Skipping this test in CICD, since it won't support it")]
      public async Task Install_WithAllowParallelism_ParallelExecutionShouldAbortResourceCreation()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem) { AllowParallelism = true };
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("config --local --list").Returns(new BufferedCommandResult(0, now, now, "gitflow", ""));
         _git.ExecAsync("config gitflow.path.hooks .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));

         // Act

         var taskList = new List<Func<Task>>()
         {
            () => command.ExecuteAsync(_console).AsTask(),
            () => command.ExecuteAsync(_console).AsTask()
         };
         await Task.WhenAll(taskList.AsParallel().Select(async q => await q()));

         // Assert
         _fileSystem.Directory.Received(1).CreateDirectory(Arg.Any<string>());

         // ReSharper disable once MethodHasAsyncOverload
         _fileSystem.File.Received(3).WriteAllText(Arg.Any<string>(), Arg.Any<string>());
      }

      [Fact(Skip = "Skipping this test in CICD, since it won't support it")]
      public async Task Install_WithoutAllowParallelism_ParallelExecutionShouldNotAbortResourceCreation()
      {
         // Arrange
         var command = new InstallCommand(_git, _cliWrap, _fileSystem) { AllowParallelism = false };
         var now = DateTimeOffset.Now;
         _git.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);
         _git.ExecAsync("config core.hooksPath .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));
         _git.ExecBufferedAsync("config --local --list").Returns(new BufferedCommandResult(0, now, now, "gitflow", ""));
         _git.ExecAsync("config gitflow.path.hooks .husky").Returns(Task.FromResult(new CommandResult(0, now, now)));

         // Act

         var taskList = new List<Func<Task>>()
         {
            () => command.ExecuteAsync(_console).AsTask(),
            () => command.ExecuteAsync(_console).AsTask(),
            () => command.ExecuteAsync(_console).AsTask(),
            () => command.ExecuteAsync(_console).AsTask()
         };
         await Task.WhenAll(taskList.AsParallel().Select(async q => await q()));

         // Assert
         _fileSystem.Directory.Received(4).CreateDirectory(Arg.Any<string>());
      }
   }
}
