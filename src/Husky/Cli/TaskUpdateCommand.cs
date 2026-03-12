using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.TaskRunner;
using Spectre.Console;

namespace Husky.Cli;

[Command("task update", Description = "Update an existing task in task-runner.json")]
public class TaskUpdateCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandOption("name", 'n', Description = "Task name to update")]
   public string? Name { get; set; }

   [CommandOption("command", 'c', Description = "New command to run")]
   public string? Command { get; set; }

   [CommandOption("args", 'a', Description = "New arguments to pass to the command")]
   public IReadOnlyList<string>? Args { get; set; }

   [CommandOption("group", 'g', Description = "New task group")]
   public string? Group { get; set; }

   [CommandOption("branch", 'b', Description = "New branch pattern (regex)")]
   public string? Branch { get; set; }

   [CommandOption("cwd", Description = "New working directory")]
   public string? Cwd { get; set; }

   [CommandOption("path-mode", Description = "New path mode (Relative, Absolute)")]
   public string? PathMode { get; set; }

   [CommandOption("output", 'o', Description = "New output mode (Always, Verbose, Never)")]
   public string? Output { get; set; }

   [CommandOption("include", Description = "New include file glob patterns")]
   public IReadOnlyList<string>? Include { get; set; }

   [CommandOption("exclude", Description = "New exclude file glob patterns")]
   public IReadOnlyList<string>? Exclude { get; set; }

   public TaskUpdateCommand(IGit git, IFileSystem fileSystem)
   {
      _git = git;
      _fileSystem = fileSystem;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var taskRunnerPath = await GetTaskRunnerPath();
      if (!_fileSystem.File.Exists(taskRunnerPath))
         throw new CommandException("task-runner.json not found, try 'husky install'");

      var content = await _fileSystem.File.ReadAllTextAsync(taskRunnerPath);
      var doc = JsonNode.Parse(content)!;
      var tasks = doc["tasks"]?.AsArray();

      if (tasks == null || tasks.Count == 0)
      {
         if (Name == null)
            throw new CommandException("No tasks found in task-runner.json");

         // Let the "not found" error occur below when Name is set
         tasks ??= new JsonArray();
      }

      // Launch interactive wizard if name not provided
      if (Name == null)
         RunInteractiveWizard(tasks);

      if (string.IsNullOrWhiteSpace(Name))
         throw new CommandException("Task name is required. Use -n <name> or run without options for interactive mode.");

      var task = tasks.FirstOrDefault(t =>
         t?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true);

      if (task == null)
         throw new CommandException($"Task '{Name}' not found in task-runner.json");

      ApplyUpdates(task.AsObject());

      await _fileSystem.File.WriteAllTextAsync(taskRunnerPath,
         doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
      $"Task '{Name}' updated in task-runner.json".Log(ConsoleColor.Green);
   }

   private void RunInteractiveWizard(JsonArray tasks)
   {
      AnsiConsole.Write(new Rule("[cyan]Update Task[/]").LeftJustified());

      var taskNames = tasks
         .Select(t => t?["name"]?.GetValue<string>() ?? "(unnamed)")
         .ToList();

      Name = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
            .Title("Select task to update:")
            .AddChoices(taskNames));

      var task = tasks.FirstOrDefault(t =>
         t?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true);

      if (task == null) return;

      var currentCommand = task["command"]?.GetValue<string>() ?? "";
      var currentArgs = task["args"]?.AsArray() is { } arr
         ? string.Join(" ", arr.Select(a => a?.GetValue<string>()))
         : "";
      var currentGroup = task["group"]?.GetValue<string>() ?? "";
      var currentCwd = task["cwd"]?.GetValue<string>() ?? "";
      var currentBranch = task["branch"]?.GetValue<string>() ?? "";
      var currentPathMode = task["pathMode"]?.GetValue<string>() ?? "";
      var currentOutput = task["output"]?.GetValue<string>() ?? "";
      var currentInclude = task["include"]?.AsArray() is { } incArr
         ? string.Join(" ", incArr.Select(a => a?.GetValue<string>()))
         : "";
      var currentExclude = task["exclude"]?.AsArray() is { } excArr
         ? string.Join(" ", excArr.Select(a => a?.GetValue<string>()))
         : "";

      var newCommand = AnsiConsole.Ask("Command:", currentCommand);
      if (newCommand != currentCommand) Command = newCommand;

      var newArgs = AnsiConsole.Ask("Arguments [grey](space-separated)[/]:", currentArgs);
      if (newArgs != currentArgs)
         Args = newArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      var newGroup = AnsiConsole.Ask("Group:", currentGroup);
      if (newGroup != currentGroup) Group = newGroup;

      var newCwd = AnsiConsole.Ask("Working directory:", currentCwd);
      if (newCwd != currentCwd) Cwd = newCwd;

      var newBranch = AnsiConsole.Ask("Branch pattern [grey](regex)[/]:", currentBranch);
      if (newBranch != currentBranch) Branch = newBranch;

      var pathModeChoices = new[] { "(keep)", nameof(PathModes.Relative), nameof(PathModes.Absolute) };
      var newPathMode = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
            .Title("Path mode:")
            .AddChoices(pathModeChoices));
      if (newPathMode != "(keep)") PathMode = newPathMode;

      var outputChoices = new[] { "(keep)", nameof(OutputTypes.Always), nameof(OutputTypes.Verbose), nameof(OutputTypes.Never) };
      var newOutput = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
            .Title("Output mode:")
            .AddChoices(outputChoices));
      if (newOutput != "(keep)") Output = newOutput;

      var newInclude = AnsiConsole.Ask("Include patterns [grey](space-separated)[/]:", currentInclude);
      if (newInclude != currentInclude)
         Include = newInclude.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      var newExclude = AnsiConsole.Ask("Exclude patterns [grey](space-separated)[/]:", currentExclude);
      if (newExclude != currentExclude)
         Exclude = newExclude.Split(' ', StringSplitOptions.RemoveEmptyEntries);
   }

   private void ApplyUpdates(JsonObject taskObj)
   {
      if (Command != null) taskObj["command"] = Command;

      if (Args != null)
      {
         var argsArray = new JsonArray();
         foreach (var arg in Args) argsArray.Add(JsonValue.Create(arg));
         taskObj["args"] = argsArray;
      }

      if (Group != null) taskObj["group"] = NullableJsonString(Group);
      if (Cwd != null) taskObj["cwd"] = NullableJsonString(Cwd);
      if (Branch != null) taskObj["branch"] = NullableJsonString(Branch);
      if (PathMode != null) taskObj["pathMode"] = PathMode;
      if (Output != null) taskObj["output"] = Output;

      if (Include != null)
      {
         var arr = new JsonArray();
         foreach (var p in Include) arr.Add(JsonValue.Create(p));
         taskObj["include"] = arr;
      }

      if (Exclude != null)
      {
         var arr = new JsonArray();
         foreach (var p in Exclude) arr.Add(JsonValue.Create(p));
         taskObj["exclude"] = arr;
      }
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }

   private static JsonNode? NullableJsonString(string value) =>
      string.IsNullOrEmpty(value) ? null : JsonValue.Create(value);
}

