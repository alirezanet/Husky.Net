using System.IO.Abstractions;
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

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage(".git can't be found (see https://alirezanet.github.io/Husky.Net/guide/getting-started)\nGit hooks installation failed");
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
   }
}
