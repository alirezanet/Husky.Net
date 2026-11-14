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
   public class AddCommandTests
   {
      private readonly FakeInMemoryConsole _console;
      private readonly IFileSystem _io;
      private readonly IGit _git;
      private readonly ICliWrap _cliWrap;
      private readonly IServiceProvider _serviceProvider;

      public AddCommandTests()
      {
         _console = new FakeInMemoryConsole();
         LoggerEx.logger = new Logger(_console);

         // sub
         _io = Substitute.For<IFileSystem>();
         _git = Substitute.For<IGit>();
         _cliWrap = Substitute.For<ICliWrap>();
         _serviceProvider = Substitute.For<IServiceProvider>();
         _serviceProvider.GetService(typeof(IFileSystem)).Returns(_io);
         _serviceProvider.GetService(typeof(IGit)).Returns(_git);
         _serviceProvider.GetService(typeof(ICliWrap)).Returns(_cliWrap);
      }

      [Fact]
      public async Task Add_WhenHuskyIsNotInstalled_ThrowException()
      {
         // Arrange
         var command = new AddCommand(_serviceProvider, _io) { Command = "-c \"echo husky.net is awesome\"", HookName = "pre-commit" };

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage($"can not find husky required files (try: husky install)");
      }

      [Fact]
      public async Task Add_WhenHookNameContainsPathSeparator_ThrowException()
      {
         // Arrange
         string hookName = $"pre-commit{Path.PathSeparator}commit";
         var command = new AddCommand(_serviceProvider, _io) { Command = "-c \"echo husky.net is awesome\"", HookName = hookName };
         _io.File.Exists(Arg.Any<string>()).Returns(true);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage($"hook name can not contain path separator");
      }

      [Fact]
      public async Task Add_WhenHookExists_NormalizesLineEndingsAfterAppendingCommand()
      {
         // Arrange
         const string huskyPath = ".husky";
         const string hookName = "pre-commit";
         var hookPath = Path.Combine(huskyPath, hookName);
         var command = new AddCommand(_serviceProvider, _io) { Command = "echo first\r\necho second", HookName = hookName };

         _git.GetHuskyPathAsync().Returns(Task.FromResult(huskyPath));
         _io.File.Exists(Path.Combine(huskyPath, "_", "husky.sh")).Returns(true);
         _io.File.Exists(hookPath).Returns(true);
         _io.File.ReadAllTextAsync(hookPath).Returns(Task.FromResult("#!/bin/sh\r\necho existing\r\n"));

         // Act
         await command.ExecuteAsync(_console);

         // Assert
         await _io.File.Received(1).WriteAllTextAsync(
            hookPath,
            "#!/bin/sh\necho existing\necho first\necho second\n");
      }
   }
}
