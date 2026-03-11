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

   // ── Regression tests: old behavior must still work ──────────────────────────

   [Fact]
   public async Task StagedVariable_WithStaticInclude_ShouldRun_WhenPatternMatchesStagedFiles()
   {
      // arrange: old behavior — ${staged} in args, plain static include glob (no ${args})
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: run without --args; staged src/Foo.cs matches **/*.cs
      var result = await c.BashAsync(output, "dotnet husky run");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task StagedVariable_WithStaticInclude_ShouldSkip_WhenPatternDoesNotMatchStagedFiles()
   {
      // arrange: old behavior — ${staged} in args, plain static include glob (no ${args})
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "**/*.ts"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: run without --args; no .ts files are staged so no match
      var result = await c.BashAsync(output, "dotnet husky run");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task NoVariable_WithStaticArgs_WithMatchingInclude_ShouldRun()
   {
      // arrange: old behavior — no variables anywhere, plain static args and include
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
                         "**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: run without --args; staged src/Foo.cs matches **/*.cs
      var result = await c.BashAsync(output, "dotnet husky run");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task StaticIncludePattern_ShouldNotBeAffectedByArgs_WhenNoArgsVariable()
   {
      // arrange: new behavior baseline — static include pattern (no ${args}),
      // verify pattern is NOT substituted even when --args is supplied
      const string taskRunner =
         """
         {
             "tasks": [
                 {
                     "name": "Echo",
                     "command": "echo",
                     "filteringRule": "staged",
                     "args": [
                         "${staged}"
                     ],
                     "include": [
                         "**/*.cs"
                     ]
                 }
             ]
         }
         """;
      await using var c = await ArrangeContainer(taskRunner);

      // act: --args is provided but the include pattern has no ${args}, so it
      // must remain a plain **/*.cs glob and still match staged src/Foo.cs
      var result = await c.BashAsync(output, "dotnet husky run --args tests");

      // assert
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task ArgsVariable_WithWildcardPattern_ShouldMatchFiles()
   {
      // arrange: test wildcards in resolved pattern
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

      // act: create a nested structure and pass the root
      await c.BashAsync("mkdir -p /test/src/nested/deep");
      await c.BashAsync("echo 'public class Nested {}' > /test/src/nested/deep/Nested.cs");
      await c.BashAsync("git add .");
      
      var result = await c.BashAsync(output, "dotnet husky run -v --args src");

      // assert: nested files should match src/**/*.cs pattern
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
      result.Stdout.Should().NotContain(DockerHelper.Skipped);
   }

   [Fact]
   public async Task ArgsVariable_MultipleDirectories_ShouldMatchCorrectly()
   {
      // arrange: test with multiple potential include directories
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

      // act: create files in different dirs, only add one to git
      await c.BashAsync("mkdir -p /test/docs");
      await c.BashAsync("echo 'public class Doc {}' > /test/docs/Doc.cs");
      await c.BashAsync("git add .");

      var result = await c.BashAsync(output, "dotnet husky run -v --args docs");

      // assert: docs files should match
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
   }

   [Fact]
   public async Task ArgsVariable_ExcludePattern_ShouldFilterCorrectly()
   {
      // arrange: test exclude pattern with ${args}
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

      // act: create additional file outside src, then exclude src
      await c.BashAsync("echo 'public class Other {}' > /test/Other.cs");
      await c.BashAsync("git add .");

      var result = await c.BashAsync(output, "dotnet husky run -v --args src");

      // assert: src files excluded, Other.cs should match
      result.ExitCode.Should().Be(0);
      result.Stdout.Should().Contain(DockerHelper.SuccessfullyExecuted);
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
