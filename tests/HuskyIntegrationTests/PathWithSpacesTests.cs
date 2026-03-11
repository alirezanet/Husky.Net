using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

/// <summary>
/// Integration tests for handling file paths with spaces in git commands.
/// Tests the fix for issue where paths like "src/Private Assemblies/Test.cs" 
/// were incorrectly split into separate arguments.
/// </summary>
public class PathWithSpacesTests(ITestOutputHelper output)
{
   [Fact]
   public async Task StagedFiles_WithSpacesInPath_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create files with spaces in directory names
      await c.BashAsync("mkdir -p 'src/Private Assemblies'");
      await c.BashAsync("echo 'test content' > 'src/Private Assemblies/Test.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test files with spaces'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
      result.Stderr.Should().Contain("src/Private Assemblies/Test.cs");
   }

   [Fact]
   public async Task StagedFiles_WithMultipleSpacesInPath_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "wc-staged",
                     "command": "wc",
                     "group": "pre-commit",
                     "args": [
                         "-l",
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create files with multiple spaces in directory names
      await c.BashAsync("mkdir -p 'My   Multiple   Spaces/Dir'");
      await c.BashAsync("echo 'line1' > 'My   Multiple   Spaces/Dir/Test.cs'");
      await c.BashAsync("echo 'line2' >> 'My   Multiple   Spaces/Dir/Test.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test multiple spaces'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
      result.Stderr.Should().NotContain("No such file or directory");
   }

   [Fact]
   public async Task StagedFiles_WithParenthesesAndSpaces_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "pathMode": "absolute",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create files with special characters and spaces
      await c.BashAsync("mkdir -p 'My (Parentheses) Dir'");
      await c.BashAsync("echo 'test' > 'My (Parentheses) Dir/Test2.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test special chars'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
      result.Stderr.Should().Contain("My (Parentheses) Dir/Test2.cs");
   }

   [Fact]
   public async Task StagedFiles_WithMixedSpecialCharsAndSpaces_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create files with mixed characters
      await c.BashAsync("mkdir -p 'My-Mixed Dashes_And_Underscores With Spaces'");
      await c.BashAsync("echo 'test' > 'My-Mixed Dashes_And_Underscores With Spaces/Test3.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test mixed chars'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
   }

   [Fact]
   public async Task StagedFiles_WithComplexPathStructure_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "pathMode": "absolute",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create the complex path from the original issue
      await c.BashAsync("mkdir -p 'src/Private Assemblies/My Controllers'");
      await c.BashAsync("echo 'controller content' > 'src/Private Assemblies/My Controllers/TestController.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test complex path'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec 'src/Private'");
      result.Stderr.Should().Contain("src/Private Assemblies/My Controllers/TestController.cs");
   }

   [Fact]
   public async Task StagedFiles_WithMultipleFilesWithSpaces_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create multiple files with spaces in different directories
      await c.BashAsync("mkdir -p 'Dir One'");
      await c.BashAsync("mkdir -p 'Dir Two'");
      await c.BashAsync("mkdir -p 'Dir Three/Sub Dir'");
      await c.BashAsync("echo 'file1' > 'Dir One/File1.cs'");
      await c.BashAsync("echo 'file2' > 'Dir Two/File2.cs'");
      await c.BashAsync("echo 'file3' > 'Dir Three/Sub Dir/File3.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test multiple files'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
      result.Stderr.Should().Contain("Dir One/File1.cs");
      result.Stderr.Should().Contain("Dir Two/File2.cs");
      result.Stderr.Should().Contain("Dir Three/Sub Dir/File3.cs");
   }

   [Fact]
   public async Task StagedFiles_RelativePathMode_WithSpaces_ShouldExecuteSuccessfully()
   {
      // arrange
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "echo-staged",
                     "command": "echo",
                     "group": "pre-commit",
                     "pathMode": "relative",
                     "args": [
                         "${staged}"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);
      
      // Create files with spaces
      await c.BashAsync("mkdir -p 'src/My Project'");
      await c.BashAsync("echo 'test' > 'src/My Project/Test.cs'");
      await c.BashAsync("git add .");

      // act
      var result = await c.BashAsync(output, "git commit -m 'test relative path'");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().NotContain("fatal: pathspec");
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
