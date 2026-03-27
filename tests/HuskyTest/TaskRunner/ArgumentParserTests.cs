using System.Runtime.InteropServices;
using FluentAssertions;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.TaskRunner;
using NSubstitute;
using Xunit;

namespace HuskyTest.TaskRunner
{
   public class ArgumentParserTests : IDisposable
   {
      private readonly IGit _git;
      private readonly string _tempDir;
      private readonly string _huskyDir;

      public ArgumentParserTests()
      {
         var console = new CliFx.Infrastructure.FakeInMemoryConsole();
         LoggerEx.logger = new Husky.Stdout.Logger(console);

         _git = Substitute.For<IGit>();
         _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
         _huskyDir = Path.Combine(_tempDir, ".husky");
         Directory.CreateDirectory(_huskyDir);

         // Default git mock setup
         _git.GetGitPathAsync().Returns(_tempDir);
         _git.GetHuskyPathAsync().Returns(".husky");
      }

      public void Dispose()
      {
         if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
      }

      private void WriteTaskRunner(string content)
      {
         File.WriteAllText(Path.Combine(_huskyDir, "task-runner.json"), content);
      }

      private (string command, string[] args) GetEchoCommand(string output)
      {
         if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("cmd", ["/c", "echo", output]);
         return ("echo", [output]);
      }

      [Fact]
      public async Task ParseAsync_WithCustomVariable_StagedFalse_ReturnsCustomVariableArgumentType()
      {
         // Arrange
         var (cmd, cmdArgs) = GetEchoCommand("test.cs");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "my-files",
                  "command": "{{cmd}}",
                  "args": [{{argsJson}}],
                  "staged": false
               }
            ],
            "tasks": []
         }
         """);

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "echo",
            Args = ["${my-files}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         args.Should().NotBeEmpty();
         args.Should().AllSatisfy(a => a.ArgumentTypes.Should().Be(ArgumentTypes.CustomVariable));
      }

      [Fact]
      public async Task ParseAsync_WithCustomVariable_StagedNotProvided_ReturnsCustomVariableArgumentType()
      {
         // Arrange
         var (cmd, cmdArgs) = GetEchoCommand("test.cs");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "my-files",
                  "command": "{{cmd}}",
                  "args": [{{argsJson}}]
               }
            ],
            "tasks": []
         }
         """);

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "echo",
            Args = ["${my-files}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         args.Should().NotBeEmpty();
         args.Should().AllSatisfy(a => a.ArgumentTypes.Should().Be(ArgumentTypes.CustomVariable));
      }

      [Fact]
      public async Task ParseAsync_WithCustomVariable_StagedTrue_ReturnsStagedFileArgumentType()
      {
         // Arrange
         var (cmd, cmdArgs) = GetEchoCommand("test.cs");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "my-files",
                  "command": "{{cmd}}",
                  "args": [{{argsJson}}],
                  "staged": true
               }
            ],
            "tasks": []
         }
         """);

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "echo",
            Args = ["${my-files}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         args.Should().NotBeEmpty();
         args.Should().AllSatisfy(a => a.ArgumentTypes.Should().Be(ArgumentTypes.StagedFile));
      }

      [Fact]
      public async Task ParseAsync_WithUnknownCustomVariable_ReturnsEmptyArgs()
      {
         // Arrange
         WriteTaskRunner("""
         {
            "variables": [],
            "tasks": []
         }
         """);

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "echo",
            Args = ["${unknown-variable}"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         args.Should().BeEmpty();
      }

      [Fact]
      public async Task ParseAsync_WithStagedVariable_NoSeparator_ReturnsStagedFileArgumentType()
      {
         // Arrange
         _git.GetStagedFilesAsync().Returns(new[] { "src/MyClass.cs" });

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "echo",
            Args = ["${staged}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         args.Should().ContainSingle();
         args[0].ArgumentTypes.Should().Be(ArgumentTypes.StagedFile);
         args[0].Should().BeOfType<FileArgumentInfo>();
         args[0].ExcludeFromCommandArgs.Should().BeFalse();
      }

      [Fact]
      public async Task ParseAsync_WithStagedSeparator_ProducesBothStaticAndStagedFileTracking()
      {
         // Arrange
         _git.GetStagedFilesAsync().Returns(new[] { "src/ClassA.cs", "src/ClassB.cs" });

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "dotnet",
            Args = ["jb", "cleanupcode", "--include=${staged:;}", "Test.sln"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         var staticArgs = args.Where(a => a.ArgumentTypes == ArgumentTypes.Static).ToArray();
         var stagedArgs = args.Where(a => a.ArgumentTypes == ArgumentTypes.StagedFile).ToArray();

         // 4 static: "jb", "cleanupcode", "--include=src/ClassA.cs;src/ClassB.cs", "Test.sln"
         staticArgs.Should().HaveCount(4);
         staticArgs[2].Argument.Should().Be("--include=src/ClassA.cs;src/ClassB.cs");

         // 2 staged file tracking entries (one per matched file)
         stagedArgs.Should().HaveCount(2);
         stagedArgs.Should().AllSatisfy(a =>
         {
            a.Should().BeOfType<FileArgumentInfo>();
            a.ExcludeFromCommandArgs.Should().BeTrue(
               "because ${staged:;} tracking entries should not appear in command args");
         });
      }

      [Fact]
      public async Task ParseAsync_WithStagedSeparator_StagedEntriesHaveCorrectRelativePaths()
      {
         // Arrange
         _git.GetStagedFilesAsync().Returns(new[] { "src/ClassA.cs", "src/ClassB.cs" });

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "dotnet",
            Args = ["--include=${staged:;}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert
         var stagedArgs = args.OfType<FileArgumentInfo>()
            .Where(a => a.ArgumentTypes == ArgumentTypes.StagedFile)
            .ToArray();

         stagedArgs.Select(a => a.RelativePath)
            .Should().BeEquivalentTo(["src/ClassA.cs", "src/ClassB.cs"]);
      }

      [Fact]
      public async Task ParseAsync_WithStagedSeparator_ResultContainsStagedFileType_EnablingRestagingRoute()
      {
         // Arrange
         _git.GetStagedFilesAsync().Returns(new[] { "src/MyClass.cs" });

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "dotnet",
            Args = ["--include=${staged:;}"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert - the presence of StagedFile type enables StagedTask routing in the factory
         args.Should().Contain(a => a.ArgumentTypes == ArgumentTypes.StagedFile,
            "because ${staged:;} should produce StagedFile argument types to enable re-staging");
      }

      [Fact]
      public async Task ParseAsync_WithStagedSeparator_NoMatchingFiles_ReturnsOnlyStaticArgs()
      {
         // Arrange
         _git.GetStagedFilesAsync().Returns(new[] { "readme.md" });

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "test",
            Command = "dotnet",
            Args = ["jb", "cleanupcode", "--include=${staged:;}", "Test.sln"],
            Include = ["**/*.cs"]
         };

         // Act
         var args = await parser.ParseAsync(huskyTask);

         // Assert - no .cs files staged, so no staged entries, just the other static args
         args.Should().HaveCount(3); // "jb", "cleanupcode", "Test.sln"
         args.Should().AllSatisfy(a => a.ArgumentTypes.Should().Be(ArgumentTypes.Static));
      }
   }
}
