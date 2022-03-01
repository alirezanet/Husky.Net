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
         var command = new AddCommand(_serviceProvider, _io) { Command = "-c \"echo husky.net is awesome\"", HookName = "pre-commit;commit" };
         _io.File.Exists(Arg.Any<string>()).Returns(true);

         // Act
         Func<Task> act = async () => await command.ExecuteAsync(_console);

         await act.Should().ThrowAsync<CommandException>().WithMessage($"hook name can not contain path separator");
      }
   }
}
