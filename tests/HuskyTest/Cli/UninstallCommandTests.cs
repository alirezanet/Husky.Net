using CliFx.Exceptions;
using CliFx.Infrastructure;
using CliWrap;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class UninstallCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;

      public UninstallCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _git = Substitute.For<IGit>();
      }

      [Fact]
      public async Task Uninstall_WhenGitFailed_ThrowException()
      {
         // Arrange
         var command = new UninstallCommand(_git);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("config --unset core.hooksPath").Returns(Task.FromResult(new CommandResult(-1, now, now)));

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("Failed to uninstall git hooks");
      }

      [Fact]
      public async Task Uninstall_Succeed()
      {
         // Arrange
         var command = new UninstallCommand(_git);
         var now = DateTimeOffset.Now;
         _git.ExecAsync("config --unset core.hooksPath").Returns(Task.FromResult(new CommandResult(0, now, now)));

         // Act
         await command.ExecuteAsync(_console);
      }
   }
}
