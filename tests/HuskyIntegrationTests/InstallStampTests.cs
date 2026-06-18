using FluentAssertions;

namespace HuskyIntegrationTests;

/// <summary>
/// Tests for the MSBuild auto-install target's incremental behavior (PR #170):
/// the target installs once (stamp existence check), and <c>dotnet clean</c>
/// removes the stamp so the next build re-installs (e.g. after a tool version bump).
/// </summary>
public class InstallStampTests(ITestOutputHelper output)
{
   private const string StampPath = ".husky/_/install.stamp";
   private static readonly string StampExists =
      $"test -f {StampPath} && echo STAMP_EXISTS || echo STAMP_MISSING";
   private static readonly string StampMtime =
      $"stat -c %Y {StampPath} 2>/dev/null || echo NONE";

   [Fact]
   public async Task DotnetClean_RemovesInstallStamp_SoNextBuildReinstalls()
   {
      // arrange: a project with the husky MSBuild target attached
      await using var c = await DockerHelper.StartWithInstalledHusky();
      await c.BashAsync("dotnet husky attach TestProjectBase.csproj");

      // act 1: the first build runs the install and writes the stamp.
      await c.BashAsync(output, "dotnet build TestProjectBase.csproj");
      var afterBuild = await c.BashAsync(output, StampExists);
      afterBuild.Stdout.Should().Contain("STAMP_EXISTS", "the first build should install husky and create the stamp");

      // act 2: dotnet clean should remove the stamp so a later build re-installs
      await c.BashAsync(output, "dotnet clean TestProjectBase.csproj");
      var afterClean = await c.BashAsync(output, StampExists);

      // assert: the stamp is gone after clean
      afterClean.Stdout.Should().Contain("STAMP_MISSING", "dotnet clean should remove the install stamp");

      // act 3 + assert: the next build re-creates it (recovery path works end to end)
      await c.BashAsync(output, "dotnet build TestProjectBase.csproj");
      var afterRebuild = await c.BashAsync(output, StampExists);
      afterRebuild.Stdout.Should().Contain("STAMP_EXISTS", "the build after clean should re-install and re-create the stamp");
   }

   [Fact]
   public async Task SecondBuild_DoesNotReinstall_WhenStampExists()
   {
      // arrange: a project with the husky MSBuild target attached
      await using var c = await DockerHelper.StartWithInstalledHusky();
      await c.BashAsync("dotnet husky attach TestProjectBase.csproj");

      // act: build twice in a row. Build the project explicitly: a solution-level
      // restore does not run the project's AfterTargets="Restore" target.
      await c.BashAsync(output, "dotnet build TestProjectBase.csproj");
      var firstMtime = (await c.BashAsync(output, StampMtime)).Stdout.Trim();
      firstMtime.Should().NotBe("NONE", "the first build should create the stamp");

      await c.BashAsync(output, "dotnet build TestProjectBase.csproj");
      var secondMtime = (await c.BashAsync(output, StampMtime)).Stdout.Trim();

      // assert: the stamp was not re-touched, so the install target ran only once
      secondMtime.Should().Be(firstMtime, "the second build should skip the install target because the stamp already exists");
   }
}
