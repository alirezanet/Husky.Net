using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky;

public static class TaskRunner
{
   public static async Task<int> Run()
   {
      "🚀 Preparing tasks ...".Husky();
      var git = new Git();
      // read tasks
      var tasks = await GetHuskyTasksAsync(git);

      foreach (var task in tasks)
      {
         // use command for task name
         if (string.IsNullOrEmpty(task.Name))
            task.Name = task.Command;

         // current working directory
         string cwd;
         if (string.IsNullOrEmpty(task.Cwd))
            cwd = Path.GetFullPath(await git.GitPath, Environment.CurrentDirectory);
         else
            cwd = Path.IsPathFullyQualified(task.Cwd) ? task.Cwd : Path.GetFullPath(task.Cwd, Environment.CurrentDirectory);

         $"⚡ Preparing task '{task.Name}'".Husky();
         if (task.Command == null) continue; // skip if no command is defined
         var args = await GetArgStringFromTask(task, git);

         if (task.Args != null && task.Args.Length > args.Count)
         {
            $"💤 Skipped, no matched files".Husky(ConsoleColor.Yellow);
            continue;
         }

         $"⌛ Executing ...".Husky();
         // execute task in order
         var result = await Utility.RunCommandAsync(task.Command, args, cwd, task.Output);
         if (result.ExitCode != 0)
         {
            Console.WriteLine();
            $"❌ Task '{task.Name}' failed".Husky(ConsoleColor.Red);
            Console.WriteLine();
            return result.ExitCode;
         }

         $" ✔ Successfully executed".Husky(ConsoleColor.DarkGreen);
      }

      "Execution completed 🐶".Husky(ConsoleColor.DarkGreen);
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

   private static async Task<IList<string>> GetArgStringFromTask(HuskyTask task, Git git)
   {
      if (task.Args == null) return new string[] { };

      // this is not lazy, because each task can have different patterns
      var matcher = GetPatternMatcher(task);
      var args = new List<string>();
      foreach (var arg in task.Args)
         switch (arg.ToLower().Trim())
         {
            case "${staged}":
               {
                  var stagedFiles = (await git.StagedFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
                  // continue if nothing is staged
                  if (!stagedFiles.Any()) continue;

                  // get match staged files with glob
                  var matches = matcher.Match(stagedFiles);
                  AddMatchFiles(task.PathMode, matches, args, await git.GitPath);
                  continue;
               }
            case "${lastCommit}":
               {
                  var lastCommitFiles = (await git.LastCommitFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
                  if (lastCommitFiles.Length < 1) continue;
                  var matches = matcher.Match(lastCommitFiles);
                  AddMatchFiles(task.PathMode, matches, args, await git.GitPath);
                  continue;
               }
            case "${matched}":
               {
                  var files = Directory.GetFiles(await git.GitPath, "*", SearchOption.AllDirectories);
                  var matches = matcher.Match(files);
                  AddMatchFiles(task.PathMode, matches, args, await git.GitPath);
                  continue;
               }
            default:
               args.Add(arg);
               break;
         }

      return args;
   }

   private static void AddMatchFiles(PathModes pathMode, PatternMatchingResult matches, ICollection<string> args, string rootPath)
   {
      if (!matches.HasMatches) return;
      var matchFiles = matches.Files.Select(q => $"{q.Path}").ToArray();
      LogMatchFiles(matchFiles);
      foreach (var f in matchFiles)
      {
         switch (pathMode)
         {
            case PathModes.Relative:
               args.Add(f);
               break;
            case PathModes.Absolute:
               args.Add(Path.GetFullPath(f, rootPath));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(HuskyTask.PathMode), pathMode,
                  "Invalid path mode. Supported modes: (relative | absolute)");
         }
      }
   }

   private static void LogMatchFiles(IEnumerable<string> files)
   {
      // show matched files in verbose mode
      if (!Logger.Verbose) return;
      "Matches:".Husky(ConsoleColor.DarkGray);
      foreach (var file in files)
         $"  {file}".LogVerbose();
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
