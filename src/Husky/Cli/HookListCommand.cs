using System.IO.Abstractions;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("hook list", Description = "List all husky hooks")]
public class HookListCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   public HookListCommand(IGit git, IFileSystem fileSystem)
   {
      _git = git;
      _fileSystem = fileSystem;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var huskyPath = await _git.GetHuskyPathAsync();

      if (!_fileSystem.File.Exists(Path.Combine(huskyPath, "_", "husky.sh")))
         throw new CommandException("can not find husky required files (try: husky install)");

      var hooks = _fileSystem.Directory.GetFiles(huskyPath)
         .Select(f => _fileSystem.Path.GetFileName(f))
         .Where(f => f != null && !f.Contains('.'))
         .OrderBy(f => f)
         .ToList();

      if (hooks.Count == 0)
      {
         "No hooks found".Log(ConsoleColor.Yellow);
         return;
      }

      foreach (var hook in hooks)
      {
         $"  - {hook}".Log();
      }
   }
}
