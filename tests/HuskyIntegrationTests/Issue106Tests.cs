using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;
public class Issue106Tests (ITestOutputHelper output)
{
   [Fact]
   public async Task EchoWithIncludeTask_WhenNoMatchFilesFound_ShouldSkip()
   {
      // arrange
      await using var c = await ArrangeContainer();
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer([CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("git add .");

      const string tasks =
         """
         {
             "tasks": [
                 {
                     "name": "EchoWithInclude",
                     "group": "pre-commit",
                     "command": "bash",
                     "filteringRule": "staged",
                     "args": [
                         "-c",
                         "echo Husky.Net is awesome!"
                     ],
                     "windows": {
                         "command": "cmd",
                         "args": [
                             "/c",
                             "echo Husky.Net is awesome!"
                         ]
                     },
                     "include": [
                         "client/**/*"
                     ]
                 }
             ]
         }
         """;
      await c.UpdateTaskRunner(tasks);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }
}
