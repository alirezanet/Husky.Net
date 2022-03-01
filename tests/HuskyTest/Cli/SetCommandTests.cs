using System.IO.Abstractions;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Cli;
using Husky.Services.Contracts;
using Husky.Stdout;
using NSubstitute;
using Xunit;

namespace HuskyTest.Cli
{
   public class SetCommandTests
   {
      private const string HuskyPath = ".husky";
      private readonly FakeInMemoryConsole _console;
      private readonly IGit _git;
      private readonly ICliWrap _cliWrap;
      private readonly IFileSystem _fileSystem;

      public SetCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _fileSystem = Substitute.For<IFileSystem>();
         _git = Substitute.For<IGit>();
         _cliWrap = Substitute.For<ICliWrap>();

         _git.GetHuskyPathAsync().Returns(HuskyPath);
      }

      [Fact]
      public async Task Set_WhenHuskyIsNotInstalled_ThrowException()
      {
         // Arrange
         var command = new SetCommand(_git, _cliWrap, _fileSystem);
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(false);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("can not find husky required files (try: husky install)");
      }

      [Fact]
      public async Task Set_WhenHookNameContainsPathSeparator_ThrowException()
      {
         // Arrange
         string hookName = $"pre-commit{Path.PathSeparator}commit";
         var command = new SetCommand(_git, _cliWrap, _fileSystem) { HookName = hookName };
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(true);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         // Assert
         await act.Should().ThrowAsync<CommandException>().WithMessage("hook name can not contain path separator");
      }

      [Fact]
      public async Task Set_Succeed()
      {
         // Arrange
         var command = new SetCommand(_git, _cliWrap, _fileSystem) { HookName = "pre-commit" };
         _fileSystem.File.Exists(Path.Combine(HuskyPath, "_", "husky.sh")).Returns(true);

         // Act
         await command.ExecuteAsync(_console);

         // Assert
         await _cliWrap.Received(1).SetExecutablePermission(Arg.Any<string>());
      }
   }
}
