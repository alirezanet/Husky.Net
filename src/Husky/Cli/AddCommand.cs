using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;

namespace Husky.Cli;

[Command("add", Description = "Add husky hook (.husky/pre-commit -c \"echo husky.net is awesome\")")]
public class AddCommand : CommandBase
{
   [CommandParameter(0, Name = "hook-file", Description = "Hook file path (e.g '.husky/pre-commit' )")]
   public string HookFile { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string cmd { get; set; } = "dotnet husky run";

   public override async ValueTask ExecuteAsync(IConsole console)
   {
      // Set if not exists
      if (!File.Exists(HookFile))
      {
         var setCommand = new SetCommand() { HookFile = HookFile, cmd = cmd };
         await setCommand.ExecuteAsync(console);
      }

      await File.AppendAllTextAsync(HookFile, $"{cmd}\n");
      $"added to '${HookFile}' hook".Log(ConsoleColor.Green);
   }
}
