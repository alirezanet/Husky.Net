using FluentAssertions;

namespace HuskyIntegrationTests;

public class Tests(DockerFixture docker, ITestOutputHelper output) : IClassFixture<DockerFixture>
{

   [Fact]
public async Task IntegrationTestCopyFolder()
{
   var c = await docker.CopyAndStartAsync(nameof(TestProjectBase));

   await c.BashAsync("git init");
   await c.BashAsync("dotnet tool restore");
   await c.BashAsync("dotnet husky install");
   var result = await c.BashAsync(output, "dotnet husky run");

   result.Stdout.Should().Contain(Extensions.SuccessfullyExecuted);
   result.ExitCode.Should().Be(0);
}

[Fact]
public async Task IntegrationTest()
{
   var c = await docker.StartAsync();

   await c.BashAsync("dotnet new classlib");
   await c.BashAsync("dotnet new tool-manifest");
   await c.BashAsync("dotnet tool install Husky");
   await c.BashAsync("git init");
   await c.BashAsync("dotnet husky install");
   var result = await c.BashAsync(output, "dotnet husky run");

   result.Stdout.Should().Contain(Extensions.SuccessfullyExecuted);
   result.ExitCode.Should().Be(0);
}
}
