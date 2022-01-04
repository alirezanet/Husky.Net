using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Stdout;

// ReSharper disable MemberCanBeMadeStatic.Global

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

   [CommandOption("vt100", Description = "Enable VT100 terminal colors", EnvironmentVariable = "vt100")]
   public bool Vt100
   {
      get => Logger.Vt100Colors;
      set => Logger.Vt100Colors = value;
   }

   public async ValueTask ExecuteAsync(IConsole console)
   {
      // catch unhandled exceptions.
      try
      {
         await SafeExecuteAsync(console);
      }
      catch (CommandException)
      {
         throw;
      }
      catch (Exception ex)
      {
         if (Verbose)
            throw;

         throw new CommandException(ex.Message, innerException: ex);
      }
   }

   protected abstract ValueTask SafeExecuteAsync(IConsole console);
}
