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
      public async Task HasVariableMatchAsync_WithMatchingFiles_ReturnsTrue()
      {
         // Arrange
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
            Include = ["**/*.cs"]
         };

         // Act
         var result = await parser.HasVariableMatchAsync(huskyTask, "cs-files");

         // Assert
         result.Should().BeTrue();
      }

      [Fact]
      public async Task HasVariableMatchAsync_WithNoMatchingFiles_ReturnsFalse()
      {
         // Arrange
         var (cmd, cmdArgs) = GetEchoCommand("src/MyComponent.ts");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "ts-files",
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
            Include = ["**/*.cs"]
         };

         // Act
         var result = await parser.HasVariableMatchAsync(huskyTask, "ts-files");

         // Assert
         result.Should().BeFalse();
      }

      [Fact]
      public async Task HasVariableMatchAsync_WithNonExistentVariable_ReturnsFalse()
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
            Include = ["**/*.cs"]
         };

         // Act
         var result = await parser.HasVariableMatchAsync(huskyTask, "non-existent-variable");

         // Assert
         result.Should().BeFalse();
      }

      [Fact]
      public async Task HasVariableMatchAsync_WithNoIncludePatterns_AndNonEmptyOutput_ReturnsTrue()
      {
         // Arrange
         var (cmd, cmdArgs) = GetEchoCommand("some-file.txt");
         var argsJson = string.Join(", ", cmdArgs.Select(a => $"\"{a}\""));
         WriteTaskRunner($$"""
         {
            "variables": [
               {
                  "name": "any-files",
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
            Name = "my-task",
            Command = "echo",
            Args = ["hello"]
            // No Include patterns - should match all files
         };

         // Act
         var result = await parser.HasVariableMatchAsync(huskyTask, "any-files");

         // Assert
         result.Should().BeTrue();
      }
   }
}
