using FluentAssertions;

namespace HuskyIntegrationTests;

/// <summary>
/// Integration tests that verify the error messages shown when git is unavailable
/// or when Husky has not been installed (dotnet husky install was not run).
/// </summary>
public class GitErrorMessageTests(ITestOutputHelper output)
{
   [Fact]
   public async Task HuskyRun_WhenHuskyIsNotInstalled_ShouldShowInstallHintInErrorMessage()
   {
      // arrange: set up git and husky tool WITHOUT running dotnet husky install
      await using var c = await DockerHelper.StartContainerAsync();
      await c.BashAsync("dotnet new tool-manifest");
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky --version 99.1.1-test");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("git init");
      // Intentionally NOT running: dotnet husky install

      // act
      var result = await c.BashAsync(output, "dotnet husky run");

      // assert
      result.ExitCode.Should().NotBe(0);
      result.Stderr.Should().Contain("dotnet husky install");
   }

   [Fact]
   public async Task HuskyRun_WhenGitIsNotAvailable_ShouldShowHelpfulErrorMessage()
   {
      // arrange: fully install husky, then remove the git binary from PATH to simulate it being missing
      await using var c = await DockerHelper.StartWithInstalledHusky();
      await c.BashAsync("mv $(which git) /tmp/git_backup");

      // act
      var result = await c.BashAsync(output, "dotnet husky run");

      // assert
      result.ExitCode.Should().NotBe(0);
      var allOutput = result.Stdout + result.Stderr;
      allOutput.Should().Contain("not found");
      allOutput.Should().Contain("PATH");
   }
}
