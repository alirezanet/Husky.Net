using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using U = Husky.Utility;

namespace Husky;

public static class CliActions
{
   private const string HUSKY_FOLDER_NAME = ".husky";

   // Custom dir help TODO: change this url to short version of docs
   public const string DOCS_URL = "https://github.com/alirezanet/husky.net";

   public static async Task<int> Install(string? dir = null)
   {
      // Ensure that we're inside a git repository
      // If git command is not found, we should return exception.
      // That's why ExitCode needs to be checked explicitly.
      if ((await Git.ExecAsync("rev-parse")).ExitCode != 0)
      {
         "Not a git repository".LogErr();
         return 1;
      }

      var cwd = Environment.CurrentDirectory;

      // set default husky folder
      dir ??= HUSKY_FOLDER_NAME;
      var path = Path.GetFullPath(Path.Combine(cwd, dir));

      // Ensure that we're not trying to install outside of cwd
      if (!path.StartsWith(cwd))
      {
         $"{cwd}\nnot allowed (see {DOCS_URL})".LogErr();
         return 1;
      }

      // Ensure that cwd is git top level
      if (!IsValidateCwd(cwd))
      {
         $".git can't be found (see {DOCS_URL})".LogErr();
         return 1;
      }

      try
      {
         // Create .husky/_
         Directory.CreateDirectory(Path.Combine(path, "_"));

         // Create .husky/_/.  ignore
         await File.WriteAllTextAsync(Path.Combine(path, "_/.gitignore"), "*");

         // Copy husky.sh to .husky/_/husky.sh
         {
            await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.husky.sh")!;
            using var sr = new StreamReader(stream);
            var content = await sr.ReadToEndAsync();
            await File.WriteAllTextAsync(Path.Combine(path, "_/husky.sh"), content);
         }

         // Created task-runner.json file
         // We don't want to override this file
         if (!File.Exists(Path.Combine(path, "task-runner.json")))
         {
            await using var stream = Assembly.GetAssembly(typeof(Program))!.GetManifestResourceStream("Husky.task-runner.json")!;
            using var sr = new StreamReader(stream);
            var content = await sr.ReadToEndAsync();
            await File.WriteAllTextAsync(Path.Combine(path, "task-runner.json"), content);
         }

         // Configure repo
         var p = await Git.ExecAsync($"config core.hooksPath {dir}");
         if (p.ExitCode != 0)
         {
            "Failed to configure git".LogErr();
            return 1;
         }
      }
      catch (Exception e)
      {
         e.Message.logVerbose(ConsoleColor.DarkRed);
         "Git hooks installation failed".LogErr();
         return 1;
      }

      "Git hooks installed".Log(ConsoleColor.Green);
      return 0;
   }

   private static bool IsValidateCwd(string cwd)
   {
      return Directory.Exists(Path.Combine(cwd, ".git"));
   }

   public static int Version()
   {
      var v = Assembly.GetAssembly(typeof(CliActions))?.GetName().Version?.ToString() ?? throw new Exception("Something is not right!");
      v.Log();
      return 0;
   }

   public static async Task<int> Uninstall()
   {
      var p = await Git.ExecAsync("config --unset core.hooksPath");
      if (p.ExitCode != 0)
      {
         "Failed to uninstall git hooks".LogErr();
         return 1;
      }

      "Git hooks successfully uninstalled".Log(ConsoleColor.Green);
      return 0;
   }

   public static async Task<int> Set(string file, string cmd)
   {
      var dir = Path.GetDirectoryName(file);
      if (!Directory.Exists(dir))
      {
         $"can't create hook, {dir} directory doesn't exist (try running husky install)".LogErr();
         return 1;
      }

      var content = @$"#!/bin/sh
. ""$(dirname ""$0"")/_/husky.sh""
## husky task runner examples -------------------

## run predefined tasks
#husky run

## or put your custom commands -------------------
#echo ""Husky.Net is awesome!""
{cmd}
";
      await File.WriteAllTextAsync(file, content);

      $"created {file}".Log(ConsoleColor.Green);
      return 0;
   }

   public static async Task<int> Add(string file, string cmd)
   {
      if (!File.Exists(file)) return await Set(file, cmd);

      await File.AppendAllTextAsync(file, $"{cmd}\n");
      "added to hook".Log();
      return 0;
   }

   public static async Task<int> Run()
   {
      "Preparing tasks".Husky();
      var git = new Git();
      // read tasks
      var tasks = await GetHuskyTasksAsync(git);

      foreach (var task in tasks)
      {
         var cwd = await git.GitPath;
         if (!string.IsNullOrEmpty(task.Cwd))
         {
            cwd = task.Cwd;
            if (!IsValidateCwd(cwd))
            {
               $"{cwd} is not a valid git repository".LogErr();
               return 1;
            }
         }

         $"Running task '{task.Name}'".Husky();
         if (task.Command == null) continue; // skip if no command is defined
         var args = await GetArgStringFromTask(task, git);

         // execute task in order
         var result = await Utility.ExecAsync(task.Command, args, cwd);
         if (result.ExitCode != 0)
         {
            $"Task '{task.Name}' failed".LogErr();
            return result.ExitCode;
         }

         $"Successfully executed '{task.Name}'".Husky(ConsoleColor.Green);
      }

      "Task execution complete".Husky(ConsoleColor.Green);
      return 0;
   }

   private static async Task<List<HuskyTask>> GetHuskyTasksAsync(Git git)
   {
      var gitPath = await git.GitPath;
      var huskyPath = await git.HuskyPath;
      var tasks = new List<HuskyTask>();
      var dir = Path.Combine(gitPath, huskyPath, "task-runner.json");
      var config = new ConfigurationBuilder()
         .AddJsonFile(dir)
         .Build();
      config.GetSection("tasks").Bind(tasks);
      return tasks;
   }

   private static async Task<IEnumerable<string>> GetArgStringFromTask(HuskyTask task, Git git)
   {
      if (task.Args == null) return new string[] { };

      // this is not lazy, because each task can have different patterns
      var matcher = GetPatternMatcher(task);
      var args = new List<string>();

      var stagedFiles = await git.StagedFiles;
      var lastCommitFiles = await git.LastCommitFiles;

      foreach (var arg in task.Args)
      {
         switch (arg.ToLower().Trim())
         {
            case "${staged}":
               {
                  // continue if nothing is staged
                  if (stagedFiles.Length < 1) continue;

                  // get match staged files with glob
                  var matchFiles = matcher.Match(stagedFiles);
                  if (matchFiles.HasMatches)
                     args.Add(string.Join(" ", matchFiles.Files.Select(q => $"'{q.Path}'")));
                  continue;
               }

            case "${lastCommit}":
               {
                  if (lastCommitFiles.Length < 1) continue;
                  var matchFiles = matcher.Match(lastCommitFiles);
                  if (matchFiles.HasMatches)
                     args.Add(string.Join(" ", matchFiles.Files.Select(q => $"{q.Path}")));
                  continue;
               }
            default:
               args.Add(arg);
               break;
         }
      }

      return args;
   }

   private static Matcher GetPatternMatcher(HuskyTask task)
   {
      var matcher = new Matcher();
      var hasMatcher = false;
      if (task.Include is { Length: > 0 })
      {
         matcher.AddIncludePatterns(task.Include);
         hasMatcher = true;
      }

      if (task.Exclude is { Length: > 0 })
      {
         matcher.AddExcludePatterns(task.Exclude);
         hasMatcher = true;
      }

      if (hasMatcher == false)
         matcher.AddInclude("**/*");

      return matcher;
   }
}
