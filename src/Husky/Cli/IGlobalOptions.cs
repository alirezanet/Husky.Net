using CliFx;
using CliFx.Attributes;
using Husky.Stdout;

namespace Husky.Cli;

public interface IGlobalOptions : ICommand
{
   [CommandOption("no-color", Description = "Disable color output")]
   public bool NoColor
   {
      get => !LoggerEx.logger.Colors;
      set => LoggerEx.logger.Colors = !value;
   }

   [CommandOption("verbose", 'v', Description = "Enable verbose output")]
   public bool Verbose
   {
      get => LoggerEx.logger.Verbose;
      set => LoggerEx.logger.Verbose = value;
   }
}
