using System.IO.Abstractions;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;

namespace Husky.Cli;

[Command("clean", Description = "Clean Husky cached files.")]
public class CleanupCommand : CommandBase
{
   private readonly IFileSystem _fileSystem;
   private readonly IGit _git;

   public CleanupCommand(IFileSystem fileSystem, IGit git)
   {
      _fileSystem = fileSystem;
      _git = git;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var cacheFolder = await GetHuskyCacheFolder();

      if (_fileSystem.Directory.Exists(cacheFolder))
      {
         _fileSystem.Directory.Delete(cacheFolder, true);
      }
   }

   internal async Task<string> GetHuskyCacheFolder()
   {
      var huskyPath = await _git.GetHuskyPathAsync();

      if (!_fileSystem.Directory.Exists(Path.Combine(huskyPath, "_")))
         throw new CommandException("can not find husky required files (try: husky install)");

      return Path.Combine(huskyPath, "_", "cache");
   }
}
