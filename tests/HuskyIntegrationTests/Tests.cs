using FluentAssertions;

namespace HuskyIntegrationTests;

public class Tests(ITestOutputHelper output)
{

   [Fact]
   public async Task IntegrationTestCopyFolder()
   {
      await using var c = await DockerHelper.StartContainerAsync(nameof(TestProjectBase));

      await c.BashAsync("git init");
      await c.BashAsync("dotnet new tool-manifest");
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky --version 99.1.1-test");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("dotnet husky install");
      var result = await c.BashAsync(output, "dotnet husky run");

      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.ExitCode.Should().Be(0);
   }

   [Fact]
   public async Task IntegrationTest()
   {
      await using var c = await DockerHelper.StartContainerAsync();

      await c.BashAsync("dotnet new classlib");
      await c.BashAsync("dotnet new tool-manifest");
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky --version 99.1.1-test");
      await c.BashAsync("git init");
      await c.BashAsync("dotnet husky install");
      var result = await c.BashAsync(output, "dotnet husky run");

      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.ExitCode.Should().Be(0);
   }

   [Fact]
   public async Task CheckVersion()
   {
      await using var c = await DockerHelper.StartWithInstalledHusky();
      var result = await c.BashAsync(output, "dotnet husky --version");

      result.Stdout.Should().Contain("99.1.1");
      result.ExitCode.Should().Be(0);
   }
}
