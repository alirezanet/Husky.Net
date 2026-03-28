using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Containers;
using FluentAssertions;

namespace HuskyIntegrationTests;

/// <summary>
/// Tests for issue #173: ${staged:separator} pattern does not re-stage files after task execution.
/// When using ${staged:;} (with separator), formatting changes made by the task should be
/// re-staged before the commit is finalized.
/// </summary>
public class Issue173Tests(ITestOutputHelper output)
{
   [Fact]
   public async Task StagedSeparator_FormattedChanges_ShouldBeCommitted()
   {
      // Arrange: set up a task using ${staged:;} that formats files via sed
      await using var c = await ArrangeContainer();

      // Create a .cs file with bad formatting (extra spaces)
      await c.AddCsharpClass(
         "public  class   MyClass  {  public   void   DoStuff (  ) {  }  }",
         "MyClass.cs");
      await c.BashAsync("git add .");

      // Act: commit - the pre-commit hook should format and re-stage the file
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // Assert: task executed successfully
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);

      // Assert: the committed file should contain the formatted content (no extra spaces)
      var committedContent = await c.BashAsync(output, "git show HEAD:MyClass.cs");
      committedContent.Stdout.Trim().Should().NotContain("  class   ");
      committedContent.Stdout.Trim().Should().Contain("class MyClass");

      // Assert: the working tree should be clean (no unstaged formatting changes left behind)
      var status = await c.BashAsync(output, "git status --porcelain");
      status.Stdout.Trim().Should().BeEmpty(
         "because formatted changes should be staged and committed, not left as unstaged modifications");
   }

   [Fact]
   public async Task StagedSeparator_MultipleFiles_FormattedChanges_ShouldAllBeCommitted()
   {
      // Arrange
      await using var c = await ArrangeContainer();

      // Create multiple .cs files with bad formatting
      await c.AddCsharpClass(
         "public  class   ClassA  {  }",
         "ClassA.cs");
      await c.AddCsharpClass(
         "public  class   ClassB  {  }",
         "ClassB.cs");
      await c.BashAsync("git add .");

      // Act
      var result = await c.BashAsync(output, "git commit -m 'add classes'");

      // Assert: task executed
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);

      // Assert: both committed files should be formatted
      var committedA = await c.BashAsync(output, "git show HEAD:ClassA.cs");
      committedA.Stdout.Trim().Should().Contain("class ClassA");
      committedA.Stdout.Trim().Should().NotContain("  class   ");

      var committedB = await c.BashAsync(output, "git show HEAD:ClassB.cs");
      committedB.Stdout.Trim().Should().Contain("class ClassB");
      committedB.Stdout.Trim().Should().NotContain("  class   ");

      // Assert: working tree clean
      var status = await c.BashAsync(output, "git status --porcelain");
      status.Stdout.Trim().Should().BeEmpty(
         "because all formatted changes should be staged and committed");
   }

   [Fact]
   public async Task StagedWithoutSeparator_FormattedChanges_ShouldBeCommitted()
   {
      // Arrange: same test but using ${staged} (no separator) to confirm it works as a control
      await using var c = await ArrangeContainerWithoutSeparator();

      await c.AddCsharpClass(
         "public  class   MyClass  {  }",
         "MyClass.cs");
      await c.BashAsync("git add .");

      // Act
      var result = await c.BashAsync(output, "git commit -m 'add MyClass.cs'");

      // Assert
      result.ExitCode.Should().Be(0);
      result.Stderr.Should().Contain(DockerHelper.SuccessfullyExecuted);

      var committedContent = await c.BashAsync(output, "git show HEAD:MyClass.cs");
      committedContent.Stdout.Trim().Should().Contain("class MyClass");
      committedContent.Stdout.Trim().Should().NotContain("  class   ");

      var status = await c.BashAsync(output, "git status --porcelain");
      status.Stdout.Trim().Should().BeEmpty();
   }

   /// <summary>
   /// Sets up a container with a pre-commit task using ${staged:;} (separator pattern).
   /// Uses sed as a lightweight formatter to replace multiple spaces with single spaces.
   /// </summary>
   private async Task<IContainer> ArrangeContainer([CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);

      // Create a formatting script that processes semicolon-separated file list
      await c.BashAsync("printf '%s\\n' '#!/bin/bash' 'IFS=\";\" read -ra FILES <<< \"$1\"' 'for file in \"${FILES[@]}\"; do' '  if [ -f \"$file\" ]; then' '    sed -i \"s/  */ /g\" \"$file\"' '  fi' 'done' > /test/format.sh && chmod +x /test/format.sh");
      await c.BashAsync("git add . && git commit -m 'add format script'");

      // Configure task-runner using ${staged:;} pattern (the bug scenario)
      const string taskRunner =
         """
         {
            "tasks": [
               {
                  "name": "format-cs",
                  "group": "pre-commit",
                  "command": "bash",
                  "pathMode": "relative",
                  "include": ["**/*.cs"],
                  "args": [
                     "/test/format.sh",
                     "${staged:;}"
                  ]
               }
            ]
         }
         """;
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }

   /// <summary>
   /// Control setup using ${staged} (no separator) — this path is known to work correctly.
   /// Each file is passed as a separate argument.
   /// </summary>
   private async Task<IContainer> ArrangeContainerWithoutSeparator([CallerMemberName] string name = null!)
   {
      var c = await DockerHelper.StartWithInstalledHusky(name);

      // Create a formatting script that processes each file argument individually
      await c.BashAsync("printf '%s\\n' '#!/bin/bash' 'for file in \"$@\"; do' '  if [ -f \"$file\" ]; then' '    sed -i \"s/  */ /g\" \"$file\"' '  fi' 'done' > /test/format.sh && chmod +x /test/format.sh");
      await c.BashAsync("git add . && git commit -m 'add format script'");

      // Configure task-runner using ${staged} (no separator - known working path)
      const string taskRunner =
         """
         {
            "tasks": [
               {
                  "name": "format-cs",
                  "group": "pre-commit",
                  "command": "bash",
                  "pathMode": "relative",
                  "include": ["**/*.cs"],
                  "args": [
                     "/test/format.sh",
                     "${staged}"
                  ]
               }
            ]
         }
         """;
      await c.UpdateTaskRunner(taskRunner);
      await c.BashAsync("dotnet husky add pre-commit -c 'dotnet husky run -g pre-commit'");
      return c;
   }
}
