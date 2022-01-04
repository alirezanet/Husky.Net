using System.Reflection;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Helpers;
using Husky.Stdout;

namespace Husky.Cli;

[Command("set", Description = "Set husky hook (pre-push -c \"dotnet test\")")]
public class SetCommand : CommandBase
{
   [CommandParameter(0, Name = "hook-name", Description = "hook name (pre-commit, commit-msg, pre-push, etc.)")]
   public string HookName { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string Command { get; set; } = "dotnet husky run";

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var huskyPath = await GetHuskyPath();
      await CreateHook(huskyPath);
   }

   private async Task CreateHook(string huskyPath)
   {
      var hookPath = Path.Combine(huskyPath, HookName);
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.hook")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await File.WriteAllTextAsync(hookPath, $"{content}\n{Command}\n");
      }

      // needed for linux
      await Utility.SetExecutablePermission(hookPath);

      $"created {hookPath}".Log(ConsoleColor.Green);
   }

   internal async Task<string> GetHuskyPath()
   {
      var git = new Git();
      var huskyPath = await git.GetHuskyPathAsync();

      if (!File.Exists(Path.Combine(huskyPath, "_", "husky.sh")))
         throw new CommandException("can not find husky required files (try: husky install)");

      if (HookName.Contains(Path.PathSeparator))
         throw new CommandException("hook name can not contain path separator", showHelp: true);
      return huskyPath;
   }
}
