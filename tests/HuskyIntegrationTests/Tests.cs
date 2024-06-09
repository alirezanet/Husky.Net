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
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky");
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
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky");
      await c.BashAsync("git init");
      await c.BashAsync("dotnet husky install");
      var result = await c.BashAsync(output, "dotnet husky run");

      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.ExitCode.Should().Be(0);
   }
}
