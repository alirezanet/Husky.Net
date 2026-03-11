using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

public class StagedCustomVariableTests(ITestOutputHelper output)
{
   [Fact]
   public async Task StagedVariable_WithMatchingFiles_ShouldExecuteAndRestageFiles()
   {
      // Arrange
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--no-ext-diff", "--diff-filter=AM"],
                     "staged": true
                 }
             ],
             "tasks": [
                 {
                     "name": "Echo staged cs files",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["${staged-cs-files}"],
                     "include": ["**/*.cs"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // add a .cs file and stage it
      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task NonStagedVariable_WithMatchingFiles_ShouldExecuteNormally()
   {
      // Arrange
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--no-ext-diff", "--diff-filter=AM"],
                     "staged": false
                 }
             ],
             "tasks": [
                 {
                     "name": "Echo staged cs files",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["${staged-cs-files}"],
                     "include": ["**/*.cs"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // add a .cs file and stage it
      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task StagedVariable_WithoutMatchingFiles_ShouldSkip()
   {
      // Arrange
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--no-ext-diff", "--diff-filter=AM"],
                     "staged": true
                 }
             ],
             "tasks": [
                 {
                     "name": "Echo staged cs files",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["${staged-cs-files}"],
                     "include": ["**/*.cs"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // Only the task-runner.json is staged (not a .cs file)
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json only'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task VariableWithoutStagedProperty_WithoutMatchingFiles_ShouldSkip()
   {
      // Arrange
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--no-ext-diff", "--diff-filter=AM"]
                 }
             ],
             "tasks": [
                 {
                     "name": "Echo staged cs files",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["${staged-cs-files}"],
                     "include": ["**/*.cs"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // Only the task-runner.json is staged (not a .cs file)
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add task-runner.json only'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }
}
