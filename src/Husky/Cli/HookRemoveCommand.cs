using System.IO.Abstractions;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("hook remove", Description = "Remove a husky hook")]
public class HookRemoveCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandParameter(0, Name = "hook-name", Description = "Hook name to remove (pre-commit, commit-msg, pre-push, etc.)")]
   public string HookName { get; set; } = default!;

   public HookRemoveCommand(IGit git, IFileSystem fileSystem)
   {
      _git = git;
      _fileSystem = fileSystem;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var huskyPath = await _git.GetHuskyPathAsync();

      if (!_fileSystem.File.Exists(Path.Combine(huskyPath, "_", "husky.sh")))
         throw new CommandException("can not find husky required files (try: husky install)");

      if (HookName.Contains(Path.DirectorySeparatorChar) || HookName.Contains(Path.AltDirectorySeparatorChar))
         throw new CommandException("hook name can not contain path separator", showHelp: true);

      var hookPath = Path.Combine(huskyPath, HookName);
      if (!_fileSystem.File.Exists(hookPath))
         throw new CommandException($"Hook '{HookName}' not found");

      _fileSystem.File.Delete(hookPath);
      $"Hook '{HookName}' removed".Log(ConsoleColor.Green);
   }
}
