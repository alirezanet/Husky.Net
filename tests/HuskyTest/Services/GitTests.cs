using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Services;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Services
{
   public class GitTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly ICliWrap _cliWrap;

      public GitTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _cliWrap = Substitute.For<ICliWrap>();
      }

      [Fact]
      public async Task GetStagedFiles_WhenTheRepositoryHasNoCommits_ReturnEmptyArray()
      {
         // Arrange
         var git = new Git(_cliWrap);
         var now = DateTime.UtcNow;
         _cliWrap.ExecBufferedAsync("git", "rev-list -n1 --all").Returns(Task.FromResult(new CliWrap.Buffered.BufferedCommandResult(0, now, now, string.Empty, string.Empty)));

         // Act
         var stagedFiles = await git.GetStagedFilesAsync();

         // Assert
         stagedFiles.Should().BeEmpty();
      }
   }
}
