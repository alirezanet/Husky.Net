using System.Text.RegularExpressions;
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

   /// <summary>
   /// Builds a pattern matcher for the given task, resolving any custom variable references
   /// (e.g. <c>${my-variable}</c>) in <c>include</c>/<c>exclude</c> patterns by executing
   /// the corresponding variable command.
   /// Returns <c>null</c> when all include patterns were custom-variable references that
   /// produced no output, signalling that the task should be skipped.
   /// </summary>
   Task<Matcher?> GetPatternMatcherAsync(HuskyTask huskyTask, string[]? optionArguments = null);
}

public partial class ArgumentParser : IArgumentParser
{
   private readonly IGit _git;
   private readonly Lazy<Task<IList<HuskyVariable>>> _customVariableTasks;

   private const string StagedWithSeparatorPattern = @".*(\$\{staged(?:\:(.+))\}).*";
#if NET7_0_OR_GREATER
   [GeneratedRegex(StagedWithSeparatorPattern, RegexOptions.Compiled)]
   private static partial Regex StagedPatternRegex();
#endif

   public ArgumentParser(IGit git)
   {
      _git = git;
      _customVariableTasks = new Lazy<Task<IList<HuskyVariable>>>(GetCustomVariableTasks);
   }

   public async Task<ArgumentInfo[]> ParseAsync(HuskyTask task, string[]? optionArguments = null)
   {
      var args = new List<ArgumentInfo>();
      if (task.Args == null)
         return Array.Empty<ArgumentInfo>();

      // this is not lazy, because each task can have different patterns
      // GetPatternMatcherAsync resolves custom-variable references in include/exclude;
      // it returns null when all include patterns were empty variables (skip signal).
      // In that case we still parse args so the Variable filtering-rule count check works.
      var matcher = await GetPatternMatcherAsync(task, optionArguments) ?? CreateEmptyMatcher();

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
#if NET7_0_OR_GREATER
            case { } x when x.Contains("${staged") && StagedPatternRegex().IsMatch(x):
            {
               var match = StagedPatternRegex().Match(x);
#else
            case { } x when x.Contains("${staged") && Regex.IsMatch(x, StagedWithSeparatorPattern, RegexOptions.Compiled):
            {
               var match = new Regex(StagedWithSeparatorPattern, RegexOptions.Compiled).Match(x);
#endif
               await AddStagedFiles(matcher, args, pathMode, match);
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

   public async Task<Matcher?> GetPatternMatcherAsync(HuskyTask task, string[]? optionArguments = null)
   {
      var matcher = new Matcher();
      var hasMatcher = false;
      var hadCustomVariableInInclude = false;

      if (task.Include is { Length: > 0 })
      {
         hadCustomVariableInInclude = task.Include.Any(IsCustomVariablePattern);
         var resolved = (await ResolvePatternVariablesAsync(task.Include, optionArguments)).ToList();
         if (resolved.Count > 0)
         {
            matcher.AddIncludePatterns(resolved);
            hasMatcher = true;
         }
      }

      // If every include entry was a custom-variable reference that resolved to nothing,
      // signal "skip this task" by returning null.
      if (hadCustomVariableInInclude && !hasMatcher)
         return null;

      if (task.Exclude is { Length: > 0 })
      {
         var resolved = (await ResolvePatternVariablesAsync(task.Exclude, optionArguments)).ToList();
         if (resolved.Count > 0)
         {
            matcher.AddExcludePatterns(resolved);
            hasMatcher = true;
         }
      }

      if (!hasMatcher)
         matcher.AddInclude("**/*");

      return matcher;
   }

   private async Task AddStagedFiles(Matcher matcher, ICollection<ArgumentInfo> args, PathModes pathMode, Match? match = null)
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

      if (match is null || !match.Success)
         AddMatchedFiles(args, pathMode, ArgumentTypes.StagedFile, matches, gitPath);
      else
         AddStaticMatchedFiles(args, pathMode, matches, gitPath, match);
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

      var huskyVariable = customVariables.Last(q => q.Name == variable);
      var gitPath = await _git.GetGitPathAsync();

      // get relative paths for matcher
      var files = (await GetCustomVariableOutput(huskyVariable))
          .Where(q => !string.IsNullOrWhiteSpace(q))
          .Select(q => Path.IsPathFullyQualified(q) ? Path.GetRelativePath(gitPath, q) : q);
      var matches = matcher.Match(gitPath, files);
      var argumentType = huskyVariable.Staged ? ArgumentTypes.StagedFile : ArgumentTypes.CustomVariable;
      AddMatchedFiles(args, pathMode, argumentType, matches, gitPath);
   }

   private async Task<IEnumerable<string>> GetCustomVariableOutput(HuskyVariable variable)
   {
      var output = Array.Empty<string>();
      try
      {
         if (variable.Command == null || variable.Args == null)
            return output;
         var cwd = await _git.GetTaskCwdAsync(variable);
         var result = await CliWrap.Cli
             .Wrap(variable.Command)
             .WithArguments(variable.Args)
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

   private async Task<IList<HuskyVariable>> GetCustomVariableTasks()
   {
      var dir = Path.Combine(
          await _git.GetGitPathAsync(),
          await _git.GetHuskyPathAsync(),
          "task-runner.json"
      );
      var variables = new List<HuskyVariable>();
      var config = new ConfigurationBuilder().AddJsonFile(dir).Build();
      config.GetSection("variables").Bind(variables);
      return variables;
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

   private static void AddStaticMatchedFiles(
      ICollection<ArgumentInfo> args,
      PathModes pathMode,
      PatternMatchingResult matches,
      string rootPath,
      Match match)
   {
      if (!matches.HasMatches)
         return;

      var matchFiles = matches.Files.Select(q => $"{q.Path}").ToArray();
      LogMatchedFiles(matchFiles);

      var fileList = matchFiles.Select(f => pathMode switch
      {
         PathModes.Relative => f,
         PathModes.Absolute => Path.GetFullPath(f, rootPath),
         _ => throw new ArgumentOutOfRangeException(nameof(HuskyTask.PathMode), pathMode,
            "Invalid path mode. Supported modes: (relative | absolute)")
      });
      var filesString = string.Join(match.Groups[2].Value, fileList);
      var arg = match.Groups[0].Value.Replace(match.Groups[1].Value, filesString);
      args.Add(new ArgumentInfo(ArgumentTypes.Static, arg));
   }

   private static void AddMatchedFiles(
      ICollection<ArgumentInfo> args,
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

   public static Matcher GetPatternMatcher(HuskyTask task, string[]? optionArguments = null)
   {
      var matcher = new Matcher();
      var hasMatcher = false;
      if (task.Include is { Length: > 0 })
      {
         matcher.AddIncludePatterns(ResolvePatternVariables(task.Include, optionArguments));
         hasMatcher = true;
      }

      if (task.Exclude is { Length: > 0 })
      {
         matcher.AddExcludePatterns(ResolvePatternVariables(task.Exclude, optionArguments));
         hasMatcher = true;
      }

      if (hasMatcher == false)
         matcher.AddInclude("**/*");

      return matcher;
   }

   private async Task<IEnumerable<string>> ResolvePatternVariablesAsync(string[] patterns, string[]? optionArguments)
   {
      var result = new List<string>();
      foreach (var pattern in patterns)
      {
         if (IsCustomVariablePattern(pattern))
         {
            // Expand custom variable to the file paths returned by its command
            var varName = pattern[2..^1];
            var customVariables = await _customVariableTasks.Value;
            if (customVariables.All(q => q.Name != varName))
            {
               $"⚠️ the custom variable '{varName}' not found in include/exclude pattern".Husky(ConsoleColor.Yellow);
               // Variable not found → contributes nothing (may trigger skip)
               continue;
            }

            var huskyVariable = customVariables.Last(q => q.Name == varName);
            var files = (await GetCustomVariableOutput(huskyVariable))
                .Where(q => !string.IsNullOrWhiteSpace(q));
            result.AddRange(files);
         }
         else if (pattern.Contains("${args}") && optionArguments != null && optionArguments.Length > 0)
         {
            foreach (var arg in optionArguments)
               result.Add(pattern.Replace("${args}", arg));
         }
         else
         {
            result.Add(pattern);
         }
      }

      return result;
   }

   private static IEnumerable<string> ResolvePatternVariables(string[] patterns, string[]? optionArguments)
   {
      foreach (var pattern in patterns)
      {
         if (pattern.Contains("${args}") && optionArguments != null && optionArguments.Length > 0)
         {
            foreach (var arg in optionArguments)
               yield return pattern.Replace("${args}", arg);
         }
         else
         {
            yield return pattern;
         }
      }
   }

   /// <summary>
   /// Returns true when the pattern is a bare <c>${variable-name}</c> reference to a custom
   /// variable (not a built-in like <c>${staged}</c>, <c>${args}</c>, etc.).
   /// </summary>
   private static bool IsCustomVariablePattern(string pattern)
   {
      if (!pattern.StartsWith("${") || !pattern.EndsWith("}"))
         return false;

      var name = pattern[2..^1];
      return name is not ("args" or "staged" or "git-files" or "all-files" or "last-commit")
             && !name.StartsWith("staged:");
   }

   /// <summary>Returns a matcher that never matches any file path.</summary>
   private static Matcher CreateEmptyMatcher()
   {
      var m = new Matcher();
      m.AddInclude("__no_match__/__no_match__");
      return m;
   }
}
