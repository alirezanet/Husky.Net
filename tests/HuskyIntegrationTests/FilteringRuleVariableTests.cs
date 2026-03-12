using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

/// <summary>
/// Tests for using custom variable references (e.g. <c>${variable-name}</c>) directly in
/// <c>include</c> / <c>exclude</c> patterns to conditionally skip tasks.
/// </summary>
public class FilteringRuleVariableTests(ITestOutputHelper output)
{
   [Fact]
   public async Task VariableInInclude_WithMatchingFiles_ShouldRunTask()
   {
      // Arrange: include uses a custom variable; variable returns .cs files that are being staged
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
                     "include": ["${staged-cs-files}"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // stage a .cs file – the variable will return it, so include resolves to that file
      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert - task should run because include variable has output
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task VariableInInclude_WithNoMatchingFiles_ShouldSkipTask()
   {
      // Arrange: task has ${staged-cs-files} as its only include pattern.
      // We only stage a .ts file, so the variable returns nothing matching .cs.
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "staged-cs-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--diff-filter=AM", "--", "*.cs"]
                 }
             ],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["dotnet test executed"],
                     "include": ["${staged-cs-files}"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // only stage a .ts file – variable will return nothing
      await c.BashAsync("echo 'const x = 1;' > /test/app.ts");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add ts file only'");

      // assert - task should be skipped because include variable returned no files
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task VariableInInclude_WithNonExistentVariable_ShouldSkipTask()
   {
      // Arrange: include references a variable that does not exist
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
                     "include": ["${non-existent-variable}"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert - task should be skipped because the variable was not found (empty output)
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task VariableInInclude_MixedWithGlob_VariableEmptyButGlobPresent_ShouldRunTask()
   {
      // Arrange: include has both a glob pattern AND a custom variable.
      // The variable returns nothing, but the glob "**/*.cs" still matches → task runs.
      const string taskRunner =
         """
         {
             "variables": [
                 {
                     "name": "extra-files",
                     "command": "git",
                     "args": ["diff", "--cached", "--name-only", "--diff-filter=AM", "--", "*.ts"]
                 }
             ],
             "tasks": [
                 {
                     "name": "dotnet-test",
                     "group": "pre-commit",
                     "command": "echo",
                     "args": ["dotnet test executed"],
                     "include": ["**/*.cs", "${extra-files}"]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // stage a .cs file (matches the glob); variable returns nothing (no .ts changes)
      await c.AddCsharpClass("public class MyClass { }", "MyClass.cs");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // assert - task should run because "**/*.cs" glob still provides include patterns
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   private async Task<IContainer> ArrangeContainer(string taskRunner, [CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }
}
