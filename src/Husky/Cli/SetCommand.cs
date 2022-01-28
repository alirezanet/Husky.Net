using System.IO.Abstractions;
using System.Reflection;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;

namespace Husky.Cli;

[Command("set", Description = "Set husky hook (set pre-push -c \"dotnet test\")")]
public class SetCommand : CommandBase
{
   private readonly IGit _git;
   private readonly ICliWrap _cliWrap;
   private readonly IFileSystem _fileSystem;

   [CommandParameter(0, Name = "hook-name", Description = "hook name (pre-commit, commit-msg, pre-push, etc.)")]
   public string HookName { get; set; } = default!;

   [CommandOption("command", 'c', Description = "command to run")]
   public string Command { get; set; } = "dotnet husky run";

   public SetCommand(IGit git, ICliWrap cliWrap, IFileSystem fileSystem)
   {
      _git = git;
      _cliWrap = cliWrap;
      _fileSystem = fileSystem;
   }

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
         await _fileSystem.File.WriteAllTextAsync(hookPath, $"{content}\n{Command}\n");
      }

      // needed for linux
      await _cliWrap.SetExecutablePermission(hookPath);

      $"created {hookPath}".Log(ConsoleColor.Green);
   }

   internal async Task<string> GetHuskyPath()
   {
      var huskyPath = await _git.GetHuskyPathAsync();

      if (!_fileSystem.File.Exists(Path.Combine(huskyPath, "_", "husky.sh")))
         throw new CommandException("can not find husky required files (try: husky install)");

      if (HookName.Contains(Path.PathSeparator))
         throw new CommandException("hook name can not contain path separator", showHelp: true);
      return huskyPath;
   }
}
