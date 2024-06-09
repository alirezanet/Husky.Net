using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

public class Issue99Tests(ITestOutputHelper output)
{
   [Fact]
   public async Task StagedFiles_ShouldPassToJbCleanup_WithASemicolonSeparator()
   {
      // arrange
      await using var c = await ArrangeContainer();

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
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task StagedFiles_ShouldSkip_WhenNoMatchFilesFound()
   {
      // arrange
      await using var c = await ArrangeContainer(installJetBrains: false);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer([CallerMemberName] string name = null!, bool installJetBrains = true)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      if (installJetBrains)
      {
         await c.BashAsync("dotnet tool install JetBrains.ReSharper.GlobalTools --version 2024.1.3");
         await c.BashAsync("dotnet tool restore");
         await c.BashAsync("git add .");
         await c.BashAsync("git commit -m 'add jb tool'");
      }

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
