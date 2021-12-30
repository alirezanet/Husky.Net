using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CliWrap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky;

public class TaskRunner
{
   private const double MAX_ARG_LENGTH = 8191;
   private readonly bool _isWindows;
   private bool _needGitIndexUpdate;

   public TaskRunner()
   {
      _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
   }

   public async Task<int> Run(IDictionary<string, string>? config = null)
   {
      "🚀 Preparing tasks ...".Husky();
      var git = new Git();

      // read tasks
      var tasks = await GetTasks(git);

      // override windows specifics if we are on windows
      if (_isWindows)
         foreach (var task in tasks.Where(q => q.Windows != null))
            OverrideWindowsSpecifics(task);

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

      // filter tasks by branch
      if (tasks.Any(q => !string.IsNullOrEmpty(q.Branch)))
      {
         var branch = await git.CurrentBranch;
         tasks = tasks.Where(q => string.IsNullOrEmpty(q.Branch) || Regex.IsMatch(branch, q.Branch)).ToList();
      }

      if (tasks.Count == 0)
      {
         "💤 Skipped, no task found".Husky();
         return 0;
      }

      foreach (var task in tasks)
      {
         Logger.Hr();

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
         var args = await ParseArguments(task, git);

         if (task.Args != null && task.Args.Length > args.Count)
         {
            "💤 Skipped, no matched files".Husky(ConsoleColor.Blue);
            continue;
         }

         double executionTime = 0;

         // on windows, there is a max command line length of 8191
         var totalCommandLength = args.Sum(q => q.arg.Length) + task.Command.Length;

         // chunk execution
         if (_isWindows && totalCommandLength > MAX_ARG_LENGTH)
         {
            var chunks = GetChunks(totalCommandLength, args);
            for (var i = 1; i <= chunks.Count; i++)
            {
               var result = await ExecuteHuskyTask($"chunk [{i}]", task, chunks.Dequeue(), cwd);
               if (result.ExitCode != 0) return result.ExitCode;
               executionTime += result.RunTime.TotalMilliseconds;
            }
         }
         else // normal execution
         {
            var result = await ExecuteHuskyTask("", task, args, cwd);
            if (result.ExitCode != 0) return result.ExitCode;
            executionTime = result.RunTime.TotalMilliseconds;
         }


         $" ✔ Successfully executed in {executionTime:n0}ms".Husky(ConsoleColor.DarkGreen);
      }

      Logger.Hr();
      "Execution completed 🐶".Husky(ConsoleColor.DarkGreen);
      Console.WriteLine();
      return 0;
   }

   private static Queue<List<(string arg, bool isFile)>> GetChunks(int totalCommandLength, IList<(string arg, bool isFile)> args)
   {
      var chunkSize = Math.Ceiling(totalCommandLength / (MAX_ARG_LENGTH / 2));
      $"The Maximum argument length '{MAX_ARG_LENGTH}' reached, splitting matched files into {chunkSize} chunks".Husky(ConsoleColor.Yellow);

      var totalFiles = args.Count(a => a.isFile);
      var totalFilePerChunk = (int)Math.Ceiling(totalFiles / chunkSize);

      var chunks = new Queue<List<(string arg, bool isFile)>>((int)chunkSize);
      for (var i = 0; i < chunkSize; i++)
      {
         var chunk = new List<(string arg, bool isFile)>();
         var fileCounter = 0;
         var skipSize = i == 0 ? 0 : i * totalFilePerChunk;
         foreach (var arg in args)
         {
            // add normal arguments
            if (!arg.isFile)
            {
               chunk.Add(arg);
               continue;
            }

            // if file already added to the chunk, skip it
            if (skipSize > 0)
            {
               skipSize -= 1;
               continue;
            }

            // add file to the chunk,
            // we should continue to the end
            // to support normal arguments after our file list if exists
            if (fileCounter >= totalFilePerChunk) continue;

            chunk.Add(arg);
            fileCounter += 1;
         }

         chunks.Enqueue(chunk);
      }

      return chunks;
   }

   private async Task<CommandResult> ExecuteHuskyTask(string chunk, HuskyTask task, IList<(string arg, bool isFile)> args, string cwd)
   {
      $"⌛ Executing task '{task.Name}' {chunk}...".Husky();
      // execute task in order
      var result = await Utility.RunCommandAsync(task.Command!, args.Select(q => q.arg), cwd, task.Output ?? OutputTypes.Always);
      if (result.ExitCode != 0)
      {
         Console.WriteLine();
         $"❌ Task '{task.Name}' failed in {result.RunTime.TotalMilliseconds:n0}ms".Husky(ConsoleColor.Red);
         Console.WriteLine();
         return result;
      }

      // in staged mode, we should update the git index
      if (!_needGitIndexUpdate) return result;
      try
      {
         await Git.ExecAsync("update-index -g");
         _needGitIndexUpdate = false;
      }
      catch (Exception)
      {
         // Silently ignore the error if happens, we don't want to break the execution
         "Can not update git index".Husky(ConsoleColor.Yellow);
      }

      return result;
   }

   private static void OverrideWindowsSpecifics(HuskyTask task)
   {
      if (task.Windows == null) return;
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
      if (task.Branch != null)
         task.Branch = task.Windows.Branch;
      if (task.Windows.PathMode != null)
         task.PathMode = task.Windows.PathMode;
   }

   private static async Task<List<HuskyTask>> GetTasks(Git git)
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

   private async Task<IList<(string arg, bool isFile)>> ParseArguments(HuskyTask task, Git git)
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
               AddMatchedFiles(pathMode, matches, args, await git.GitPath);
               _needGitIndexUpdate = true;
               continue;
            }
            case "${lastCommit}":
            {
               var lastCommitFiles = (await git.LastCommitFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
               if (lastCommitFiles.Length < 1) continue;
               var matches = matcher.Match(lastCommitFiles);
               AddMatchedFiles(pathMode, matches, args, await git.GitPath);
               continue;
            }
            case "${matched}":
            {
               var gitPath = await git.GitPath;
               var files = Directory.GetFiles(gitPath, "*", SearchOption.AllDirectories);

               // exclude .git directory (absolute path)
               var gitDir = await git.GitDirRelativePath;
               matcher.AddExclude($"{gitDir}/**");

               var matches = matcher.Match(gitPath, files);
               AddMatchedFiles(pathMode, matches, args, gitPath);
               continue;
            }
            default:
               args.Add((arg, false));
               break;
         }

      return args;
   }

   private static void AddMatchedFiles(PathModes pathMode, PatternMatchingResult matches, ICollection<(string, bool)> args, string rootPath)
   {
      if (!matches.HasMatches) return;
      var matchFiles = matches.Files.Select(q => $"{q.Path}").ToArray();
      LogMatchedFiles(matchFiles);
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

   private static void LogMatchedFiles(IEnumerable<string> files)
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
