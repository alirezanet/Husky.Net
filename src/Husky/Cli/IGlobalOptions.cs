using CliFx;
using CliFx.Attributes;
using Husky.Stdout;

namespace Husky.Cli;

public interface IGlobalOptions : ICommand
{
   [CommandOption("no-color", 'c', Description = "Disable color output")]
   public bool NoColor
   {
      get => !Logger.Colors;
      set => Logger.Colors = !value;
   }

   [CommandOption("verbose", 'v', Description = "Enable verbose output")]
   public bool Verbose
   {
      get => Logger.Verbose;
      set => Logger.Verbose = value;
   }
}
