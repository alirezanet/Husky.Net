using CliFx.Exceptions;
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
      public async Task GetStagedFiles_WhenGitReturnExitCodeDifferentThanZero_ThrowCommandException()
      {
         // Arrange
         var git = new Git(_cliWrap);
         var now = DateTime.UtcNow;
         _cliWrap.ExecBufferedAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(new CliWrap.Buffered.BufferedCommandResult(1, now, now, string.Empty, string.Empty)));

         // Act
         Func<Task> act = async () => await git.GetStagedFilesAsync();

         // Assert
         await act.Should()
            .ThrowAsync<CommandException>()
            .WithMessage("Could not find the staged files");
      }

      [Fact]
      public async Task GetStagedFiles_Return_StagedFiles()
      {
         // Arrange
         var git = new Git(_cliWrap);
         var now = DateTime.UtcNow;
         var gitOutput = $"myfile.cs{Environment.NewLine}mysecondfile.cs{Environment.NewLine}src\\mythirdfile.cs";
         _cliWrap.ExecBufferedAsync("git", "diff --staged --name-only --no-ext-diff --diff-filter=AM").Returns(Task.FromResult(new CliWrap.Buffered.BufferedCommandResult(0, now, now, gitOutput, string.Empty)));

         // Act
         var stagedFiles = await git.GetStagedFilesAsync();

         // Assert
         stagedFiles.Should()
            .NotBeEmpty()
            .And
            .HaveCount(3)
            .And
            .Contain(new List<string>
            {
               "myfile.cs",
               "mysecondfile.cs",
               "src\\mythirdfile.cs"
            });
      }
   }
}
