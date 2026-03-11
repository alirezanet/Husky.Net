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

      // act: run with --args src (the include pattern becomes src/**/*.cs which matches)
      var result = await c.BashAsync(output, "dotnet husky run --args src");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
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

      // act: run with --args tests (the include pattern becomes tests/**/*.cs which does NOT match)
      var result = await c.BashAsync(output, "dotnet husky run --args tests");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task ArgsVariable_InExcludePattern_ShouldSkip_WhenExcludedByArgs()
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
                         "**/*.cs"
                     ],
                     "exclude": [
                         "${args}/**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: run with --args src (the exclude pattern becomes src/**/*.cs which excludes src/Foo.cs)
      var result = await c.BashAsync(output, "dotnet husky run --args src");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task ArgsVariable_InExcludePattern_ShouldNotSkip_WhenNotExcludedByArgs()
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
                         "**/*.cs"
                     ],
                     "exclude": [
                         "${args}/**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: run with --args tests (the exclude pattern becomes tests/**/*.cs which does NOT exclude src/Foo.cs)
      var result = await c.BashAsync(output, "dotnet husky run --args tests");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("mkdir -p /test/src");
      await c.BashAsync("echo 'public class Foo {}' > /test/src/Foo.cs");
      await c.BashAsync("git add .");
      return c;
   }
}
