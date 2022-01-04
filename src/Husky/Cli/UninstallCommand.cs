using System.Reflection;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Helpers;
using Husky.Stdout;

namespace Husky.Cli;

[Command("uninstall", Description = "Uninstall Husky hooks")]
public class UninstallCommand : CommandBase
{
   public override async ValueTask ExecuteAsync(IConsole console)
   {
      // TODO: Uninstall git flow hooks
      var p = await Git.ExecAsync("config --unset core.hooksPath");
      if (p.ExitCode != 0)
         throw new CommandException("Failed to uninstall git hooks");

      "Git hooks successfully uninstalled".Log(ConsoleColor.Green);
   }
}
