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

      [Fact]
      public async Task Install_WithParallelism_ShouldNotInterleaveGitCalls()
      {
         // Arrange
         var configInProgress = false;
         var interleaved = false;
         var configEntered = new SemaphoreSlim(0, 1);
         var configCanExit = new SemaphoreSlim(0, 1);
         var now = DateTimeOffset.Now;

         var cliWrap = Substitute.For<ICliWrap>();
         var fileSystem = Substitute.For<IFileSystem>();
         fileSystem.Directory.Exists(Path.Combine(Environment.CurrentDirectory, ".git")).Returns(true);

         // Install A mocks: blocks inside git config to hold the mutex open
         var gitA = Substitute.For<IGit>();
         gitA.ExecAsync("rev-parse").Returns(Task.FromResult(new CommandResult(0, now, now)));
         gitA.IsSubmodule(Arg.Any<string>()).Returns(Task.FromResult(false));
         gitA.ExecAsync(Arg.Is<string>(s => s.StartsWith("config core.hooksPath")))
            .Returns(async _ =>
            {
               configInProgress = true;
               configEntered.Release();
               await configCanExit.WaitAsync();
               configInProgress = false;
               return new CommandResult(0, now, now);
            });
         gitA.ExecBufferedAsync("config --local --list")
            .Returns(new BufferedCommandResult(0, now, now, "", ""));

         // Install B mocks: detect if reads run while A's config is in progress
         var gitB = Substitute.For<IGit>();
         gitB.ExecAsync("rev-parse").Returns(_ =>
         {
            if (configInProgress) interleaved = true;
            return Task.FromResult(new CommandResult(0, now, now));
         });
         gitB.IsSubmodule(Arg.Any<string>()).Returns(_ =>
         {
            if (configInProgress) interleaved = true;
            return Task.FromResult(false);
         });
         gitB.ExecAsync(Arg.Is<string>(s => s.StartsWith("config core.hooksPath")))
            .Returns(Task.FromResult(new CommandResult(0, now, now)));
         gitB.ExecBufferedAsync("config --local --list")
            .Returns(new BufferedCommandResult(0, now, now, "", ""));

         var commandA = new InstallCommand(gitA, cliWrap, fileSystem) { AllowParallelism = true };
         var commandB = new InstallCommand(gitB, cliWrap, fileSystem) { AllowParallelism = true };
         var consoleA = new FakeInMemoryConsole();
         var consoleB = new FakeInMemoryConsole();

         // Act: start A, wait for it to be inside git config, then start B
         var taskA = Task.Run(() => commandA.ExecuteAsync(consoleA).AsTask());
         await configEntered.WaitAsync();

         var taskB = Task.Run(() => commandB.ExecuteAsync(consoleB).AsTask());
         configCanExit.Release();
         await Task.WhenAll(taskA, taskB);

         // Assert
         interleaved.Should().BeFalse(
            "git read operations (rev-parse, IsSubmodule) should not run while another " +
            "process is writing git config, as this causes 'Permission denied' errors");
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
