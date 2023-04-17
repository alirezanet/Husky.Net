using CliWrap.Buffered;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky.TaskRunner;

public interface IArgumentParser
{
   Task<ArgumentInfo[]> ParseAsync(HuskyTask huskyTask, string[]? optionArguments = null);
}

public class ArgumentParser : IArgumentParser
{
   private readonly IGit _git;
   private readonly Lazy<Task<IList<HuskyTask>>> _customVariableTasks;

   public ArgumentParser(IGit git)
   {
      _git = git;
      _customVariableTasks = new Lazy<Task<IList<HuskyTask>>>(GetCustomVariableTasks);
   }

   public async Task<ArgumentInfo[]> ParseAsync(HuskyTask task, string[]? optionArguments = null)
   {
      var args = new List<ArgumentInfo>();
      if (task.Args == null)
         return Array.Empty<ArgumentInfo>();

      // this is not lazy, because each task can have different patterns
      var matcher = GetPatternMatcher(task);

      // set default pathMode value
      var pathMode = task.PathMode ?? PathModes.Relative;
      foreach (var arg in task.Args)
      {
         switch (arg.ToLower().Trim())
         {
            case "${args}":
               AddCustomArguments(args, optionArguments);
               break;
            case var value when value.Contains("${args}") && optionArguments is not null && optionArguments.Any():
               args.Add(new ArgumentInfo(ArgumentTypes.Static, value.Replace("${args}", string.Join(' ', optionArguments!))));
               break;
            case "${last-commit}":
            {
               await AddLastCommit(matcher, args, pathMode);
               break;
            }
            case "${git-files}":
            {
               await AddGitFiles(matcher, args, pathMode);
               break;
            }
            case "${all-files}":
            {
               await AddAllFiles(matcher, args, pathMode);
               break;
            }
            case "${staged}":
            {
               await AddStagedFiles(matcher, args, pathMode);
               break;
            }
            case { } x when x.StartsWith("${") && x.EndsWith("}"):
            {
               await AddCustomVariable(x, matcher, args, pathMode);
               break;
            }
            default:
               args.Add(new ArgumentInfo(ArgumentTypes.Static, arg));
               break;
         }
      }

      return args.ToArray();
   }

   private async Task AddStagedFiles(Matcher matcher, List<ArgumentInfo> args, PathModes pathMode)
   {
      var stagedFiles = (await _git.GetStagedFilesAsync())
          .Where(q => !string.IsNullOrWhiteSpace(q))
          .ToArray();
      // continue if nothing is staged
      if (!stagedFiles.Any())
         return;

      // get match staged files with glob
      var matches = matcher.Match(stagedFiles);
      var gitPath = await _git.GetGitPathAsync();
      AddMatchedFiles(args, pathMode, ArgumentTypes.StagedFile, matches, gitPath);
   }

   private async Task AddCustomVariable(
       string x,
       Matcher matcher,
       List<ArgumentInfo> args,
       PathModes pathMode
   )
   {
      var customVariables = await _customVariableTasks.Value;
      var variable = x[2..^1];

      // check if variable is defined
      if (customVariables.All(q => q.Name != variable))
      {
         $"⚠️ the custom variable '{variable}' not found".Husky(ConsoleColor.Yellow);
         return;
      }

      var huskyVariableTask = customVariables.Last(q => q.Name == variable);
      var gitPath = await _git.GetGitPathAsync();

      // get relative paths for matcher
      var files = (await GetCustomVariableOutput(huskyVariableTask))
          .Where(q => !string.IsNullOrWhiteSpace(q))
          .Select(q => Path.IsPathFullyQualified(q) ? Path.GetRelativePath(gitPath, q) : q);
      var matches = matcher.Match(gitPath, files);
      AddMatchedFiles(args, pathMode, ArgumentTypes.CustomVariable, matches, gitPath);
   }

   private async Task<IEnumerable<string>> GetCustomVariableOutput(HuskyTask task)
   {
      var output = Array.Empty<string>();
      try
      {
         if (task.Command == null || task.Args == null)
            return output;
         var cwd = await _git.GetTaskCwdAsync(task);
         var result = await CliWrap.Cli
             .Wrap(task.Command)
             .WithArguments(task.Args)
             .WithWorkingDirectory(cwd)
             .ExecuteBufferedAsync();
         if (result.ExitCode == 0)
            return result.StandardOutput.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
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
      var dir = Path.Combine(
          await _git.GetGitPathAsync(),
          await _git.GetHuskyPathAsync(),
          "task-runner.json"
      );
      var tasks = new List<HuskyTask>();
      var config = new ConfigurationBuilder().AddJsonFile(dir).Build();
      config.GetSection("variables").Bind(tasks);
      return tasks;
   }

   private async Task AddAllFiles(Matcher matcher, List<ArgumentInfo> args, PathModes pathMode)
   {
      var gitPath = await _git.GetGitPathAsync();
      var files = Directory.GetFiles(gitPath, "*", SearchOption.AllDirectories);

      // exclude .git directory (absolute path)
      var gitDir = await _git.GetGitDirRelativePathAsync();
      matcher.AddExclude($"{gitDir}/**");

      var matches = matcher.Match(gitPath, files);
      AddMatchedFiles(args, pathMode, ArgumentTypes.File, matches, gitPath);
   }

   private async Task AddGitFiles(Matcher matcher, List<ArgumentInfo> args, PathModes pathMode)
   {
      var gitFiles = await _git.GitFilesAsync();
      if (gitFiles.Length < 1)
         return;
      var matches = matcher.Match(gitFiles);
      var gitPath = await _git.GetGitPathAsync();
      AddMatchedFiles(args, pathMode, ArgumentTypes.File, matches, gitPath);
   }

   private async Task AddLastCommit(Matcher matcher, List<ArgumentInfo> args, PathModes pathMode)
   {
      var lastCommitFiles = (await _git.GetLastCommitFilesAsync())
          .Where(q => !string.IsNullOrWhiteSpace(q))
          .ToArray();
      if (lastCommitFiles.Length < 1)
         return;
      var matches = matcher.Match(lastCommitFiles);
      var gitPath = await _git.GetGitPathAsync();
      AddMatchedFiles(args, pathMode, ArgumentTypes.File, matches, gitPath);
   }

   private static void AddMatchedFiles(
       List<ArgumentInfo> args,
       PathModes pathMode,
       ArgumentTypes argumentType,
       PatternMatchingResult matches,
       string rootPath
   )
   {
      if (!matches.HasMatches)
         return;
      var matchFiles = matches.Files.Select(q => $"{q.Path}").ToArray();
      LogMatchedFiles(matchFiles);
      foreach (var f in matchFiles)
         switch (pathMode)
         {
            case PathModes.Relative:
               args.Add(new FileArgumentInfo(argumentType, pathMode, f));
               break;
            case PathModes.Absolute:
               args.Add(
                   new FileArgumentInfo(
                       argumentType,
                       pathMode,
                       f,
                       Path.GetFullPath(f, rootPath)
                   )
               );
               break;
            default:
               throw new ArgumentOutOfRangeException(
                   nameof(HuskyTask.PathMode),
                   pathMode,
                   "Invalid path mode. Supported modes: (relative | absolute)"
               );
         }
   }

   private static void LogMatchedFiles(IEnumerable<string> files)
   {
      // show matched files in verbose mode
      if (!LoggerEx.logger.Verbose)
         return;
      "Matches:".Husky(ConsoleColor.DarkGray);
      foreach (var file in files)
         $"  {file}".LogVerbose();
   }

   private static void AddCustomArguments(List<ArgumentInfo> args, string[]? optionArguments)
   {
      if (optionArguments != null)
         args.AddRange(
             optionArguments.Select(q => new ArgumentInfo(ArgumentTypes.CustomArgument, q))
         );
      else
         "⚠️ No arguments passed to the run command".Husky(ConsoleColor.Yellow);
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
