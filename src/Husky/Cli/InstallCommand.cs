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

   [CommandOption("ignore-submodule", Description = "Ignore installation when target is a git submodule")]
   public bool IgnoreSubmodule { get; set; } = false;

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
      "Checking git path and working directory".LogVerbose();

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

      // Check if we're in a submodule (issue #69)
      if (await _git.IsSubmodule(cwd))
      {
         // We're in a submodule
         if (IgnoreSubmodule)
         {
            // Option 1: this project has its own hooks and install target but is to be ignored when it's a submodule of some other git repo.
            "Submodule detected and [--ignore-when-submodule] is set, skipping install target".Log(ConsoleColor.Yellow);
            return;
         }
         // Option 2: this project has its own hooks and install target, it is a submodule of some other git repo
         // we attach the hooks to the submodule .git pointed at by the module's .git file. (../.git/modules/Submodule)
         $"Submodule detected, attaching {path} hooks to {await _git.GetGitDirectory(cwd)}".Log(ConsoleColor.Yellow);
      }
      else if (!_fileSystem.Directory.Exists(Path.Combine(cwd, ".git"))) // Ensure that cwd is git top level
      {
         // Need to check if we're inside a git work tree or not (issue #43)
         // If we are we can skip installation and if we're not, we should return exception.
         if (!(await _git.GetGitDirectory(cwd)).Contains("worktrees"))
            throw new CommandException($".git can't be found (see {DOCS_URL})\n" + FailedMsg);
      }

      if (AllowParallelism)
      {
         // breaks if another instance already running
         if (RunUnderMutexControl(path))
         {
            "Resource creation skipped due to multiple executions".LogVerbose();
            return;
         }
      }
      else
      {
         await CreateResourcesAsync(path);
      }

      "Git hooks installed".Log(ConsoleColor.Green);
   }

   private bool RunUnderMutexControl(string path)
   {
      using var mutex = new Mutex(false, "Global\\" + appGuid);
      if (!mutex.WaitOne(0, false))
      {
         // another instance is already running
         return true;
      }

      try
      {
         CreateResources(path);
      }
      finally
      {
         mutex.ReleaseMutex();
      }

      return false;
   }

   private void CreateResources(string path)
   {
      $"Creating resources and configuration files in '{path}'".LogVerbose();

      // Create .husky/_
      _fileSystem.Directory.CreateDirectory(Path.Combine(path, "_"));

      // Create .husky/_/.  ignore
      _fileSystem.File.WriteAllText(Path.Combine(path, "_/.gitignore"), "*");

      // Copy husky.sh to .husky/_/husky.sh
      var husky_shPath = Path.Combine(path, "_", "husky.sh");
      {
         using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.husky.sh")!;
         using var sr = new StreamReader(stream);
         var content = sr.ReadToEnd();
         _fileSystem.File.WriteAllText(husky_shPath, content);
      }

      // here we have to run the `ConfigureGitAndFilePermission` synchronously because mutex will fail if thread changes
      ConfigureGitAndFilePermission(path, husky_shPath).GetAwaiter().GetResult();

      // Created task-runner.json file
      // We don't want to override this file
      if (!_fileSystem.File.Exists(Path.Combine(path, "task-runner.json")))
      {
         using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.task-runner.json")!;
         using var sr = new StreamReader(stream);
         var content = sr.ReadToEnd();
         _fileSystem.File.WriteAllText(Path.Combine(path, "task-runner.json"), content);
      }
   }

   private async Task CreateResourcesAsync(string path)
   {
      $"Creating resources and configuration files asynchronously in '{path}'".LogVerbose();

      // Create .husky/_
      _fileSystem.Directory.CreateDirectory(Path.Combine(path, "_"));

      // Create .husky/_/.  ignore
      await _fileSystem.File.WriteAllTextAsync(Path.Combine(path, "_/.gitignore"), "*");

      // Copy husky.sh to .husky/_/husky.sh
      var husky_shPath = Path.Combine(path, "_", "husky.sh");
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.husky.sh")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await _fileSystem.File.WriteAllTextAsync(husky_shPath, content);
      }

      await ConfigureGitAndFilePermission(path, husky_shPath);

      // Created task-runner.json file
      // We don't want to override this file
      var taskRunnerJsonPath = Path.Combine(path, "task-runner.json");
      $"Creating task-runner.json in '{taskRunnerJsonPath}'".LogVerbose();

      if (!_fileSystem.File.Exists(taskRunnerJsonPath))
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.task-runner.json")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await _fileSystem.File.WriteAllTextAsync(taskRunnerJsonPath, content);
      }
   }

   private async Task ConfigureGitAndFilePermission(string path, string husky_shPath)
   {
      $"Configuring Git and File permissions in '{husky_shPath}'".LogVerbose();

      // find all hooks (if exists) from .husky/ and add executable flag
      var files = _fileSystem.Directory.GetFiles(path).Where(f => !_fileSystem.FileInfo.New(f).Name.Contains('.')).ToList();
      files.Add(husky_shPath);
      await _cliWrap.SetExecutablePermission(files.ToArray());

      // Configure repo
      var p = await _git.ExecAsync($"config core.hooksPath {HuskyDirectory}");
      if (p.ExitCode != 0)
         throw new CommandException("Failed to configure git\n" + FailedMsg);

      // Configure gitflow repo
      var local = await _git.ExecBufferedAsync("config --local --list");

      // ReSharper disable once InvertIf
      if (local.ExitCode == 0 && local.StandardOutput.Contains("gitflow"))
      {
         var gf = await _git.ExecAsync($"config gitflow.path.hooks {HuskyDirectory}");
         if (gf.ExitCode != 0)
            throw new CommandException("Failed to configure gitflow\n" + FailedMsg);
      }
   }
}
