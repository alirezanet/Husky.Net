using System.Reflection;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Helpers;
using Husky.Stdout;

namespace Husky.Cli;

[Command("set", Description = "Set husky hook (.husky/pre-push -c \"dotnet test\")")]
public class SetCommand : CommandBase
{
   [CommandParameter(0, Name = "hook-file", Description = "hook file path (e.g '.husky/pre-commit' )")]
   public string HookFile { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string cmd { get; set; } = "dotnet husky run";

   public override async ValueTask ExecuteAsync(IConsole console)
   {
      var dir = Path.GetDirectoryName(HookFile);
      if (!Directory.Exists(dir))
         throw new CommandException($"can't create hook, {dir} directory doesn't exist (try running husky install)");

      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.hook")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await File.WriteAllTextAsync(HookFile, $"{content}\n{cmd}\n");
      }

      // needed for linux
      await Utility.SetExecutablePermission(HookFile);

      $"created {HookFile}".Log(ConsoleColor.Green);
   }
}
