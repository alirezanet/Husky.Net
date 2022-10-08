using System.IO.Abstractions;
using System.Reflection;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("install", Description = "Install Husky hooks")]
public class InstallCommand : CommandBase
{
   private readonly IGit _git;
   private readonly ICliWrap _cliWrap;
   private readonly IFileSystem _fileSystem;
   private const string FailedMsg = "Git hooks installation failed";
   private const string HUSKY_FOLDER_NAME = ".husky";
   private const string DOCS_URL = "https://alirezanet.github.io/Husky.Net/guide/getting-started";

   [CommandOption("dir", 'd', Description = "The custom directory to install Husky hooks.")]
   public string HuskyDirectory { get; set; } = HUSKY_FOLDER_NAME;

   [CommandOption("parallel", 'p', Description = "If True, husky resource creation will run once " +
                                                 "to prevent locks, when multiple instances are running")]
   public bool AllowParallelism { get; set; } = true;

   // using for mutex
   private const string appGuid = "085a64e3-0998-4202-ab59-17b1ed287f6e";

   public InstallCommand(IGit git, ICliWrap cliWrap, IFileSystem fileSystem)
   {
      _git = git;
      _cliWrap = cliWrap;
      _fileSystem = fileSystem;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      // Ensure that we're inside a git repository
      // If git command is not found, we should return exception.
      // That's why ExitCode needs to be checked explicitly.
      if ((await _git.ExecAsync("rev-parse")).ExitCode != 0) throw new CommandException(FailedMsg);

      var cwd = Environment.CurrentDirectory;

      // set default husky folder
      var path = Path.GetFullPath(Path.Combine(cwd, HuskyDirectory));

      // Ensure that we're not trying to install outside of cwd
      if (!path.StartsWith(cwd))
         throw new CommandException($"{path}\nNot allowed (see {DOCS_URL})\n" + FailedMsg);

      // Ensure that cwd is git top level
      if (!_fileSystem.Directory.Exists(Path.Combine(cwd, ".git")))
      {
         // Need to check if we're inside a git work tree or not (issue #43)
         // If we are we can skip installation and if we're not, we should return exception.
         if (!(await _git.ExecBufferedAsync("rev-parse --git-dir")).StandardOutput.Contains("worktrees"))
            throw new CommandException($".git can't be found (see {DOCS_URL})\n" + FailedMsg);
      }

      if (AllowParallelism)
      {
         // breaks if another instance already running
         if (await RunUnderMutexControl(path))
         {
            "Resource creation skipped due to multiple executions".LogVerbose();
            return;
         }
      }
      else
      {
         await CreateResources(path);
      }

      "Git hooks installed".Log(ConsoleColor.Green);
   }

   private async Task<bool> RunUnderMutexControl(string path)
   {
      using var mutex = new Mutex(false, "Global\\" + appGuid);
      if (!mutex.WaitOne(0, false))
      {
         // another instance is already running
         return true;
      }

      try
      {
         await CreateResources(path);
      }
      finally
      {
         mutex.ReleaseMutex();
      }

      return false;
   }

   private async Task CreateResources(string path)
   {
      // Create .husky/_
      _fileSystem.Directory.CreateDirectory(Path.Combine(path, "_"));

      // Create .husky/_/.  ignore
      await _fileSystem.File.WriteAllTextAsync(Path.Combine(path, "_/.gitignore"), "*");

      // Copy husky.sh to .husky/_/husky.sh
      var husky_shPath = Path.Combine(path, "_/husky.sh");
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.husky.sh")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await _fileSystem.File.WriteAllTextAsync(husky_shPath, content);
      }

      // find all hooks (if exists) from .husky/ and add executable flag
      var files = _fileSystem.Directory.GetFiles(path).Where(f => !_fileSystem.FileInfo.FromFileName(f).Name.Contains('.')).ToList();
      files.Add(husky_shPath);
      await _cliWrap.SetExecutablePermission(files.ToArray());

      // Created task-runner.json file
      // We don't want to override this file
      if (!_fileSystem.File.Exists(Path.Combine(path, "task-runner.json")))
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.task-runner.json")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await _fileSystem.File.WriteAllTextAsync(Path.Combine(path, "task-runner.json"), content);
      }

      // Configure repo
      var p = await _git.ExecAsync($"config core.hooksPath {HuskyDirectory}");
      if (p.ExitCode != 0)
         throw new CommandException("Failed to configure git\n" + FailedMsg);

      // Configure gitflow repo
      var local = await _git.ExecBufferedAsync("config --local --list");
      if (local.ExitCode == 0 && local.StandardOutput.Contains("gitflow"))
      {
         var gf = await _git.ExecAsync($"config gitflow.path.hooks {HuskyDirectory}");
         if (gf.ExitCode != 0)
            throw new CommandException("Failed to configure gitflow\n" + FailedMsg);
      }
   }
}
