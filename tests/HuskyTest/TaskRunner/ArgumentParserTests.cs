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
      public async Task GetPatternMatcherAsync_WithVariableInInclude_AndMatchingOutput_ReturnsMatcher()
      {
         // Arrange: variable returns a .cs file path which becomes the include pattern
         var (cmd, cmdArgs) = GetEchoCommand("src/MyClass.cs");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "cs-files",
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
            Name = "dotnet-test",
            Command = "dotnet",
            Args = ["test"],
            Include = ["${cs-files}"]
         };

         // Act
         var matcher = await parser.GetPatternMatcherAsync(huskyTask);

         // Assert - should return a non-null matcher (variable had output)
         matcher.Should().NotBeNull();
      }

      [Fact]
      public async Task GetPatternMatcherAsync_WithVariableInInclude_AndEmptyOutput_ReturnsNull()
      {
         // Arrange: variable returns nothing; include only contains the variable ref
         var (cmd, cmdArgs) = GetEchoCommand(string.Empty);
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "empty-files",
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
            Name = "dotnet-test",
            Command = "dotnet",
            Args = ["test"],
            Include = ["${empty-files}"]
         };

         // Act
         var matcher = await parser.GetPatternMatcherAsync(huskyTask);

         // Assert - null signals "should skip" (all include patterns were empty variable refs)
         matcher.Should().BeNull();
      }

      [Fact]
      public async Task GetPatternMatcherAsync_MixedInclude_VariableEmptyButGlobPresent_ReturnsMatcher()
      {
         // Arrange: mixed include - glob stays even when variable is empty
         var (cmd, cmdArgs) = GetEchoCommand(string.Empty);
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "empty-files",
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
            Name = "dotnet-test",
            Command = "dotnet",
            Args = ["test"],
            Include = ["**/*.cs", "${empty-files}"]  // glob remains even if variable is empty
         };

         // Act
         var matcher = await parser.GetPatternMatcherAsync(huskyTask);

         // Assert - not null because the glob "**/*.cs" still provides a pattern
         matcher.Should().NotBeNull();
      }

      [Fact]
      public async Task GetPatternMatcherAsync_WithNonExistentVariable_ReturnsNull()
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
            Name = "dotnet-test",
            Command = "dotnet",
            Args = ["test"],
            Include = ["${non-existent-variable}"]
         };

         // Act
         var matcher = await parser.GetPatternMatcherAsync(huskyTask);

         // Assert - null because variable not found → treated as empty
         matcher.Should().BeNull();
      }

      [Fact]
      public async Task GetPatternMatcherAsync_WithNoInclude_ReturnsMatcher()
      {
         // Arrange: no include patterns at all → default "**/*" matcher
         WriteTaskRunner("""
         {
            "variables": [],
            "tasks": []
         }
         """);

         var parser = new ArgumentParser(_git);
         var huskyTask = new HuskyTask
         {
            Name = "my-task",
            Command = "echo",
            Args = ["hello"]
         };

         // Act
         var matcher = await parser.GetPatternMatcherAsync(huskyTask);

         // Assert - returns a valid matcher (match-all by default)
         matcher.Should().NotBeNull();
      }
   }
}
