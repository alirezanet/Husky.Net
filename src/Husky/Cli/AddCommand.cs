using System.IO.Abstractions;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;
using Microsoft.Extensions.DependencyInjection;

namespace Husky.Cli;

[Command("add", Description = "Add husky hook (add pre-commit -c \"echo husky.net is awesome\")")]
public class AddCommand : CommandBase
{
   private readonly IServiceProvider _provider;
   private readonly IFileSystem _fileSystem;

   public AddCommand(IServiceProvider provider, IFileSystem fileSystem)
   {
      _provider = provider;
      _fileSystem = fileSystem;
   }

   [CommandParameter(0, Name = "hook-name", Description = "Hook name (pre-commit, commit-msg, pre-push, etc.)")]
   public string HookName { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string Command { get; set; } = "dotnet husky run";

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var setCommand = ActivatorUtilities.CreateInstance<SetCommand>(_provider);
      setCommand.HookName = HookName;
      setCommand.Command = Command;

      var huskyPath = await setCommand.GetHuskyPath();
      var hookPath = Path.Combine(huskyPath, HookName);

      // Set if not exists
      if (!_fileSystem.File.Exists(hookPath))
      {
         await setCommand.ExecuteAsync(console);
         return;
      }

      await _fileSystem.File.AppendAllTextAsync(hookPath, $"{Command}\n");
      $"added to '{hookPath}' hook".Log(ConsoleColor.Green);
   }
}
