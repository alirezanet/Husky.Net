using FluentAssertions;

namespace HuskyIntegrationTests;

public class Tests : IClassFixture<DockerFixture>
{
   private readonly DockerFixture _docker;
   private readonly ITestOutputHelper _output;

   public Tests(DockerFixture docker, ITestOutputHelper output)
   {
      _docker = docker;
      _output = output;
      // TestcontainersSettings.Logger = new DockerLogger(output);
   }

   [Fact]
   public async Task IntegrationTestCopyFolder()
   {
      var c = await _docker.CopyAndStartAsync(nameof(TestProjectBase));

      await c.BashAsync("git init");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("dotnet husky install");
      var result = await c.BashAsync(_output, "dotnet husky run");

      result.Stdout.Should().Contain("✔ Successfully executed");
      result.ExitCode.Should().Be(0);
   }

   [Fact]
   public async Task IntegrationTest()
   {
      var c = await _docker.StartAsync();

      await c.BashAsync("dotnet new classlib");
      await c.BashAsync("dotnet new tool-manifest");
      await c.BashAsync("dotnet tool install Husky");
      await c.BashAsync("git init");
      await c.BashAsync("dotnet husky install");
      var result = await c.BashAsync(_output, "dotnet husky run");

      result.Stdout.Should().Contain("✔ Successfully executed");
      result.ExitCode.Should().Be(0);
   }
}
