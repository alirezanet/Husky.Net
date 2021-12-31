using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky;

public class TaskRunner
{
   private const double MAX_ARG_LENGTH = 8191;
   private readonly bool _isWindows;
   private bool _needGitIndexUpdate;
   private readonly Lazy<Task<IList<HuskyTask>>> _customVariableTasks;
   private readonly Git _git;

   public TaskRunner()
   {
      _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
      _git = new Git();
      _customVariableTasks = new Lazy<Task<IList<HuskyTask>>>(GetCustomVariableTasks);
   }

   public async Task<int> Run(IDictionary<string, string>? config = null)
   {
      "🚀 Preparing tasks ...".Husky();

      // read tasks
      var tasks = await GetTasks();

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
         var branch = await _git.CurrentBranch;
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
         var cwd = await GetTaskCwd(task);

         $"⚡ Preparing task '{task.Name}'".Husky();
         if (task.Command == null)
         {
            "💤 Skipped, no command found".Husky(ConsoleColor.Blue);
            continue;
         }

         string? configArgs = null;
         config?.TryGetValue("args", out configArgs);
         var args = await ParseArguments(task, configArgs);

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
      return 0;
   }

   private async Task<string> GetTaskCwd(HuskyTask task)
   {
      string cwd;
      if (string.IsNullOrEmpty(task.Cwd))
         cwd = Path.GetFullPath(await _git.GitPath, Environment.CurrentDirectory);
      else
         cwd = Path.IsPathFullyQualified(task.Cwd) ? task.Cwd : Path.GetFullPath(task.Cwd, Environment.CurrentDirectory);
      return cwd;
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

   private async Task<CommandResult> ExecuteHuskyTask(string chunk, HuskyTask task, IEnumerable<(string arg, bool isFile)> args, string cwd)
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

   private async Task<List<HuskyTask>> GetTasks()
   {
      var gitPath = await _git.GitPath;
      var huskyPath = await _git.HuskyPath;
      var tasks = new List<HuskyTask>();
      var dir = Path.Combine(gitPath, huskyPath, "task-runner.json");
      var config = new ConfigurationBuilder()
         .AddJsonFile(dir)
         .Build();
      config.GetSection("tasks").Bind(tasks);
      return tasks;
   }

   private async Task<IList<(string arg, bool isFile)>> ParseArguments(HuskyTask task, string? configArgs = null)
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
            case "${args}":
               if (configArgs != null)
                  args.Add((configArgs, false));
               else
                  "No arguments passed to the run command".Husky(ConsoleColor.Yellow);
               continue;
            case "${staged}":
            {
               var stagedFiles = (await _git.StagedFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
               // continue if nothing is staged
               if (!stagedFiles.Any()) continue;

               // get match staged files with glob
               var matches = matcher.Match(stagedFiles);
               AddMatchedFiles(pathMode, matches, args, (await _git.GitPath));
               _needGitIndexUpdate = true;
               continue;
            }
            case "${last-commit}":
            {
               var lastCommitFiles = (await _git.LastCommitFiles).Where(q => !string.IsNullOrWhiteSpace(q)).ToArray();
               if (lastCommitFiles.Length < 1) continue;
               var matches = matcher.Match(lastCommitFiles);
               AddMatchedFiles(pathMode, matches, args, await _git.GitPath);
               continue;
            }
            case "${git-files}":
            {
               var gitFiles = await _git.GitFiles;
               if (gitFiles.Length < 1) continue;
               var matches = matcher.Match(gitFiles);
               AddMatchedFiles(pathMode, matches, args, await _git.GitPath);
               continue;
            }
            case "${all-files}":
            {
               var gitPath = await _git.GitPath;
               var files = Directory.GetFiles(gitPath, "*", SearchOption.AllDirectories);

               // exclude .git directory (absolute path)
               var gitDir = await _git.GitDirRelativePath;
               matcher.AddExclude($"{gitDir}/**");

               var matches = matcher.Match(gitPath, files);
               AddMatchedFiles(pathMode, matches, args, gitPath);
               continue;
            }
            case { } x when x.StartsWith("${") && x.EndsWith("}"):
            {
               var customVariables = await _customVariableTasks.Value;
               var variable = x.Substring(2, x.Length - 3);

               // check if variable is defined
               if (customVariables.All(q => q.Name != variable))
               {
                  $"the custom variable '{variable}' not found".LogErr();
                  continue;
               }

               var huskyVariableTask = customVariables.First(q => q.Name == variable);
               var gitPath = await _git.GitPath;

               // get relative paths for matcher
               var files = (await GetCustomVariableOutput(huskyVariableTask))
                  .Where(q => !string.IsNullOrWhiteSpace(q))
                  .Select(q => Path.IsPathFullyQualified(q) ? Path.GetRelativePath(gitPath, q) : q);
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

   private async Task<IEnumerable<string>> GetCustomVariableOutput(HuskyTask task)
   {
      var output = Array.Empty<string>();
      try
      {
         OverrideWindowsSpecifics(task);
         if (task.Command == null || task.Args == null) return output;
         var cwd = await GetTaskCwd(task);

         var fullPath = Utility.GetFullyQualifiedPath(task.Command);
         var result = await CliWrap.Cli.Wrap(fullPath)
            .WithArguments(task.Args)
            .WithWorkingDirectory(cwd)
            .ExecuteBufferedAsync();
         if (result.ExitCode == 0)
            return result.StandardOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         return output;
      }

      return output;
   }

   private async Task<IList<HuskyTask>> GetCustomVariableTasks()
   {
      var dir = Path.Combine(await _git.GitPath, await _git.HuskyPath, "task-runner.json");
      var tasks = new List<HuskyTask>();
      var config = new ConfigurationBuilder()
         .AddJsonFile(dir)
         .Build();
      config.GetSection("variables").Bind(tasks);
      return tasks;
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
