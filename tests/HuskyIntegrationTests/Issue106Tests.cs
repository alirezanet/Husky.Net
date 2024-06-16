using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;
public class Issue106Tests (ITestOutputHelper output)
{
   [Fact]
   public async Task FilteringRuleNotDefined_WithInclude_ShouldNotSkip()
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
                         "Husky.Net is awesome!"
                     ],
                     "include": [
                         "client/**/*"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleNotDefined_WithStagedVariable_WithExcludeCommitedFile_ShouldSkip()
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
                     "exclude": [
                         "**/task-runner.json"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleVariable_WithStagedVariable_WithExcludeCommitedFile_ShouldSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "variable",
                     "args": [
                         "${staged}"
                     ],
                     "exclude": [
                         "**/task-runner.json"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleVariable_WithStagedVariable_WithIncludeCommitedFile_ShouldNotSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "pathMode": "absolute",
                     "filteringRule": "variable",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "**/task-runner.json"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain(DockerHelper.Skipped);
      result.Stderr.Should().Contain(".husky/task-runner.json");
   }

   [Fact]
   public async Task FilteringRuleStaged_WithoutAnyMatchedFile_ShouldSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "Husky.Net is awesome!"
                     ],
                     "include": [
                         "client/**/*"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleStaged_WithoutIncludeAndExclude_ShouldNotSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "Husky.Net is awesome!"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleStaged_WithExcludeCommitedFile_ShouldSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "Husky.Net is awesome!"
                     ],
                     "exclude": [
                         "**/task-runner.json"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleStaged_WithIncludeCommitedFile_ShouldNotSkip()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "Husky.Net is awesome!"
                     ],
                     "include": [
                         "**/task-runner.json"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain(DockerHelper.Skipped);
   }


   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("git add .");
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run'");
      return c;
   }
}
