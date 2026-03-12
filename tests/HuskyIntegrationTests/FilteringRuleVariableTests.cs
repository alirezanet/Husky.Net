using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

public class FilteringRuleVariableTests(ITestOutputHelper output)
{
   [Fact]
   public async Task FilteringRuleVariable_WithMatchingFiles_ShouldRunTask()
   {
      // Arrange
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--diff-filter=AM"]
                 }
             ],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["dotnet test executed"],
                     "include": ["**/*.cs"],
                     "filteringRuleVariable": "staged-cs-files"
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // stage a .cs file - should trigger the task
      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert - task should run because variable returns matching .cs files
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task FilteringRuleVariable_WithNoMatchingFiles_ShouldSkipTask()
   {
      // Arrange - task only runs for .cs files but we'll only stage a .ts file
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--diff-filter=AM"]
                 }
             ],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["dotnet test executed"],
                     "include": ["**/*.cs"],
                     "filteringRuleVariable": "staged-cs-files"
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // only stage a .ts file - should NOT trigger the .cs task
      await c.BashAsync("echo 'const x = 1;' > /test/app.ts");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add app.ts only'");

      // assert - task should be skipped because variable returns no .cs files
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleVariable_WithNonExistentVariable_ShouldSkipTask()
   {
      // Arrange - filteringRuleVariable references a variable that doesn't exist
      const string taskRunner =
         """
         {
             "variables": [],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["dotnet test executed"],
                     "include": ["**/*.cs"],
                     "filteringRuleVariable": "non-existent-variable"
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert - task should be skipped because the variable is not found
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task FilteringRuleVariable_WithoutVariableInArgs_ShouldFilterByVariableOutput()
   {
      // This test specifically validates the new feature:
      // A task with NO variable in its args can still be filtered by filteringRuleVariable.
      // Previously, the only way to filter by variable was to include the variable in args.

      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--diff-filter=AM"]
                 }
             ],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["running dotnet test"],
                     "include": ["**/*.cs"],
                     "filteringRuleVariable": "staged-cs-files"
                 }
             ]
         }
         """;
      // Note: "args" does NOT contain "${staged-cs-files}" - it only has static args.
      // The task should still be filtered by the variable's output.

      await using var c = await ArrangeContainer(taskRunner);

      // Stage only non-.cs files
      await c.BashAsync("echo 'const x = 1;' > /test/app.ts");
      await c.BashAsync("git add .");

      var skipResult = await c.BashAsync(output, "git commit -m 'add ts file - should skip'");
      skipResult.ExitCode.Should().Be(0);
      skipResult.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }
}
