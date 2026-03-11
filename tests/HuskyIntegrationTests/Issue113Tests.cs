using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

public class Issue113Tests(ITestOutputHelper output)
{
   [Fact]
   public async Task ArgsVariable_InIncludePattern_ShouldMatchFilesUnderArgsDirectory()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "${args}/**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // add a C# file inside the "src" subdirectory
      await c.BashAsync("mkdir -p /test/src");
      await c.BashAsync("echo 'public class Foo {}' > /test/src/Foo.cs");
      await c.BashAsync("git add .");

      // act: run with --args src (the include pattern becomes src/**/*.cs which matches)
      var result = await c.BashAsync(output, "dotnet husky run --args src");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task ArgsVariable_InIncludePattern_ShouldSkip_WhenNoMatchedFiles()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "${args}/**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // add a C# file inside the "src" subdirectory
      await c.BashAsync("mkdir -p /test/src");
      await c.BashAsync("echo 'public class Foo {}' > /test/src/Foo.cs");
      await c.BashAsync("git add .");

      // act: run with --args tests (the include pattern becomes tests/**/*.cs which does NOT match)
      var result = await c.BashAsync(output, "dotnet husky run --args tests");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("git add .");
      await c.UpdateTaskRunner(taskRunner);
      return c;
   }
}
