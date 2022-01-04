using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;

namespace Husky.Cli;

[Command("add", Description = "Add husky hook (pre-commit -c \"echo husky.net is awesome\")")]
public class AddCommand : CommandBase
{
   [CommandParameter(0, Name = "hook-name", Description = "Hook name (pre-commit, commit-msg, pre-push, etc.)")]
   public string HookName { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string cmd { get; set; } = "dotnet husky run";

   public override async ValueTask ExecuteAsync(IConsole console)
   {
      var setCommand = new SetCommand() { HookName = HookName, Command = cmd };
      var huskyPath = await setCommand.GetHuskyPath();
      var hookPath = Path.Combine(huskyPath, HookName);

      // Set if not exists
      if (!File.Exists(hookPath))
         await setCommand.ExecuteAsync(console);

      await File.AppendAllTextAsync(hookPath, $"{cmd}\n");
      $"added to '${hookPath}' hook".Log(ConsoleColor.Green);
   }
}
