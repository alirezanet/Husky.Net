using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;

namespace Husky.Cli;

[Command("uninstall", Description = "Uninstall Husky hooks")]
public class UninstallCommand : CommandBase
{
   private readonly IGit _git;

   public UninstallCommand(IGit git)
   {
      _git = git;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      // TODO: Uninstall git flow hooks
      var p = await _git.ExecAsync("config --unset core.hooksPath");
      if (p.ExitCode != 0)
         throw new CommandException("Failed to uninstall git hooks");

      "Git hooks successfully uninstalled".Log(ConsoleColor.Green);
   }
}
