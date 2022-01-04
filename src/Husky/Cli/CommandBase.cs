using System.Runtime.ExceptionServices;
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
   public bool Verbose { get; set; } = false;

   protected abstract ValueTask SafeExecuteAsync(IConsole console);

   public async ValueTask ExecuteAsync(IConsole console)
   {
      // catch unhandled exceptions.
      try
      {
         Logger.Verbose = Verbose;
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
}
