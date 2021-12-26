using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky;

public static class TaskRunner
{
   public static async Task<int> Run(IDictionary<string, string>? config = null)
   {
      "🚀 Preparing tasks ...".Husky();
      var git = new Git();
      // read tasks
      var tasks = await GetHuskyTasksAsync(git);

      // handle run arguments
      if (config != null)
      {
         if (config.ContainsKey("name"))
         {
            $"🔍 Using task name '{config["name"]}'".Husky();
            tasks = tasks.Where(q => q.Name == config["name"]).ToList();
         }

         if (config.ContainsKey("group"))
         {
            $"🔍 Using task group '{config["group"]}'".Husky();
            tasks = tasks.Where(q => q.Group == config["group"]).ToList();
         }
      }

      if (tasks.Count == 0)
      {
         "💤 Skipped, no task found".Husky();
         return 0;
      }

      foreach (var task in tasks)
      {
         Logger.Hr();
         OverrideWindowsSpecifics(task);

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
            "💤 Skipped, no matched files".Husky(ConsoleColor.Yellow);
            continue;
         }

         "⌛ Executing ...".Husky();
         // execute task in order
         var result = await Utility.RunCommandAsync(task.Command, args.Select(q => q.arg), cwd, task.Output ?? OutputTypes.Always);
         if (result.ExitCode != 0)
         {
            Console.WriteLine();
            $"❌ Task '{task.Name}' failed in {result.RunTime.TotalMilliseconds:n0}ms".Husky(ConsoleColor.Red);
            Console.WriteLine();
            return result.ExitCode;
         }

         // we must add the changed files to git
         var reAdd = await Utility.ExecAsync("git", new[] { "add" }.Concat(args.Where(q => q.isFile).Select(q => q.arg)));
         if (reAdd.ExitCode != 0)
            "Can not update git index".LogVerbose(ConsoleColor.DarkRed);


         $" ✔ Successfully executed in {result.RunTime.TotalMilliseconds:n0}ms".Husky(ConsoleColor.DarkGreen);
      }
      Logger.Hr();
      "Execution completed 🐶".Husky(ConsoleColor.DarkGreen);
      Console.WriteLine();
      return 0;
   }

   private static void OverrideWindowsSpecifics(HuskyTask task)
   {
      if (task.Windows == null) return;
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

      if (task.Windows.Cwd != null)
         task.Cwd = task.Windows.Cwd;
      if (task.Windows.Args != null)
         task.Args = task.Windows.Args;
      if (task.Windows.Command != null)
         task.Command = task.Windows.Command;
      if (task.Windows.Group != null)
         task.Group = task.Windows.Group;
      if (task.Windows.Name != null)
         task.Name = task.Windows.Name;
      if (task.Windows.Exclude != null)
         task.Exclude = task.Windows.Exclude;
      if (task.Windows.Include != null)
         task.Include = task.Windows.Include;
      if (task.Windows.Output != null)
         task.Output = task.Windows.Output;
      if (task.Windows.PathMode != null)
         task.PathMode = task.Windows.PathMode;
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

   private static async Task<IList<(string arg, bool isFile)>> GetArgStringFromTask(HuskyTask task, Git git)
   {
      var args = new List<(string arg, bool isFile)>();
      if (task.Args == null) return args;

      // this is not lazy, because each task can have different patterns
      var matcher = GetPatternMatcher(task);

      // set default pathMode value
      var pathMode = task.PathMode ?? PathModes.Relative;

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
               AddMatchFiles(pathMode, matches, args, await git.GitPath);
               continue;
            }
            case "${lastCommit}":
            {
               var lastCommitFiles = (await git.LastCommitFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
               if (lastCommitFiles.Length < 1) continue;
               var matches = matcher.Match(lastCommitFiles);
               AddMatchFiles(pathMode, matches, args, await git.GitPath);
               continue;
            }
            case "${matched}":
            {
               var files = Directory.GetFiles(await git.GitPath, "*", SearchOption.AllDirectories);
               var matches = matcher.Match(files);
               AddMatchFiles(pathMode, matches, args, await git.GitPath);
               continue;
            }
            default:
               args.Add((arg, false));
               break;
         }

      return args;
   }

   private static void AddMatchFiles(PathModes pathMode, PatternMatchingResult matches, ICollection<(string, bool)> args, string rootPath)
   {
      if (!matches.HasMatches) return;
      var matchFiles = matches.Files.Select(q => $"{q.Path}").ToArray();
      LogMatchFiles(matchFiles);
      foreach (var f in matchFiles)
         switch (pathMode)
         {
            case PathModes.Relative:
               args.Add((f, true));
               break;
            case PathModes.Absolute:
               args.Add((Path.GetFullPath(f, rootPath), true));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(HuskyTask.PathMode), pathMode,
                  "Invalid path mode. Supported modes: (relative | absolute)");
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
