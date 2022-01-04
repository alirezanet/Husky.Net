using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;

namespace Husky.Cli;

[Command]
public abstract class CommandBase : ICommand
{
   [CommandOption("verbose", 'v', Description = "Enable verbose output")]
   public bool Verbose
   {
      get => Logger.Verbose;
      set => Logger.Verbose = value;
   }

   public abstract ValueTask ExecuteAsync(IConsole console);
}
