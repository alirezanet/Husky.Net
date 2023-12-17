using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

[Collection("docker fixture")]
public class Issue99Tests(DockerFixture docker, ITestOutputHelper output) : IClassFixture<DockerFixture>
{
   [Fact]
   public async Task StagedFiles_ShouldPassToJbCleanup_WithASemicolonSeparator()
   {
      // arrange
      var c = await ArrangeContainer();

      // add 4 c# files
      for (var i = 2; i <= 4; i++)
      {
         var csharpFile =
            $$"""
              public class Class{{i}} {
              public static void TestMethod() { }
              }
              """;

         await c.AddCsharpClass(csharpFile, $"Class{i}.cs");
      }

      await c.BashAsync("git add .");

// act
      var result = await c.BashAsync(output, "git commit -m 'add 4 new csharp classes'");

// assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(Extensions.SuccessfullyExecuted);
   }

   [Fact]
   public async Task StagedFiles_ShouldSkip_WhenNoMatchFilesFound()
   {
      // arrange
      var c = await ArrangeContainer();
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(Extensions.Skipped);
   }

   private async Task<IContainer> ArrangeContainer([CallerMemberName] string name = null!)
   {
      var c = await docker.StartWithInstalledHusky(name);
      await c.BashAsync("dotnet tool install JetBrains.ReSharper.GlobalTools");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("git add .");
      await c.BashAsync("git commit -m 'add jb tool'");

      const string tasks =
         """
         {
         "tasks": [
               {
                  "name": "jb cleanup",
                  "group": "pre-commit",
                  "command": "dotnet",
                  "pathMode": "relative",
                  "include": ["**/*.cs", "**/*.vb", "*.cs"],
                  "args": [
                     "jb",
                     "cleanupcode",
                     "--include=${staged:;}",
                     "TestProjectBase.sln"
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
