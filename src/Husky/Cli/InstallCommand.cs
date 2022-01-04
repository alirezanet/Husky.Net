using System.Reflection;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Helpers;
using Husky.Stdout;

namespace Husky.Cli;

[Command("install", Description = "Install Husky hooks")]
public class InstallCommand : CommandBase
{
   private const string failedMsg = "\nGit hooks installation failed";

   [CommandOption("dir", 'd', Description = "The custom directory to install Husky hooks.")]
   public string HuskyDirectory { get; set; } = Utility.HUSKY_FOLDER_NAME;

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      // Ensure that we're inside a git repository
      // If git command is not found, we should return exception.
      // That's why ExitCode needs to be checked explicitly.
      if ((await Git.ExecAsync("rev-parse")).ExitCode != 0) throw new CommandException("Not a git repository" + failedMsg);

      var cwd = Environment.CurrentDirectory;

      // set default husky folder
      var path = Path.GetFullPath(Path.Combine(cwd, HuskyDirectory));

      // Ensure that we're not trying to install outside of cwd
      if (!path.StartsWith(cwd))
         throw new CommandException($"{path}\nNot allowed (see {Utility.DOCS_URL})" + failedMsg);

      // Ensure that cwd is git top level
      if (!Directory.Exists(Path.Combine(cwd, ".git")))
         throw new CommandException($".git can't be found (see {Utility.DOCS_URL})" + failedMsg);

      // Create .husky/_
      Directory.CreateDirectory(Path.Combine(path, "_"));

      // Create .husky/_/.  ignore
      await File.WriteAllTextAsync(Path.Combine(path, "_/.gitignore"), "*");

      // Copy husky.sh to .husky/_/husky.sh
      var husky_shPath = Path.Combine(path, "_/husky.sh");
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.husky.sh")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await File.WriteAllTextAsync(husky_shPath, content);
      }

      // find all hooks (if exists) from .husky/ and add executable flag
      var files = Directory.GetFiles(path).Where(f => !f.Contains(".")).ToList();
      files.Add(husky_shPath);
      await Utility.SetExecutablePermission(files.ToArray());

      // Created task-runner.json file
      // We don't want to override this file
      if (!File.Exists(Path.Combine(path, "task-runner.json")))
      {
         await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.templates.task-runner.json")!;
         using var sr = new StreamReader(stream);
         var content = await sr.ReadToEndAsync();
         await File.WriteAllTextAsync(Path.Combine(path, "task-runner.json"), content);
      }

      // Configure repo
      var p = await Git.ExecAsync($"config core.hooksPath {HuskyDirectory}");
      if (p.ExitCode != 0)
         throw new CommandException("Failed to configure git" + failedMsg);

      // Configure gitflow repo
      var local = await Git.ExecBufferedAsync("config --local --list");
      if (local.ExitCode == 0 && local.StandardOutput.Contains("gitflow"))
      {
         var gf = await Git.ExecAsync($"config gitflow.path.hooks {HuskyDirectory}");
         if (gf.ExitCode != 0)
            throw new CommandException("Failed to configure gitflow" + failedMsg);
      }

      "Git hooks installed".Log(ConsoleColor.Green);
   }
}
