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

[Command("task add", Description = "Add a task to task-runner.json")]
public class TaskAddCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandOption("name", 'n', Description = "Task name")]
   public string? Name { get; set; }

   [CommandOption("command", 'c', Description = "Command to run")]
   public string? Command { get; set; }

   [CommandOption("args", 'a', Description = "Arguments to pass to the command")]
   public IReadOnlyList<string>? Args { get; set; }

   [CommandOption("group", 'g', Description = "Task group")]
   public string? Group { get; set; }

   [CommandOption("branch", 'b', Description = "Branch pattern (regex) to limit task execution")]
   public string? Branch { get; set; }

   [CommandOption("cwd", Description = "Working directory for the task")]
   public string? Cwd { get; set; }

   [CommandOption("path-mode", Description = "Path mode for staged files (Relative, Absolute)")]
   public string? PathMode { get; set; }

   [CommandOption("output", 'o', Description = "Output mode (Always, Verbose, Never)")]
   public string? Output { get; set; }

   [CommandOption("include", Description = "Include file glob patterns")]
   public IReadOnlyList<string>? Include { get; set; }

   [CommandOption("exclude", Description = "Exclude file glob patterns")]
   public IReadOnlyList<string>? Exclude { get; set; }

   public TaskAddCommand(IGit git, IFileSystem fileSystem)
   {
      _git = git;
      _fileSystem = fileSystem;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var taskRunnerPath = await GetTaskRunnerPath();
      if (!_fileSystem.File.Exists(taskRunnerPath))
         throw new CommandException("task-runner.json not found, try 'husky install'");

      // Launch interactive wizard if name or command not provided
      if (Name == null || Command == null)
         RunInteractiveWizard();

      if (string.IsNullOrWhiteSpace(Name))
         throw new CommandException("Task name is required. Use -n <name> or run without options for interactive mode.");
      if (string.IsNullOrWhiteSpace(Command))
         throw new CommandException("Command is required. Use -c <command> or run without options for interactive mode.");

      var content = await _fileSystem.File.ReadAllTextAsync(taskRunnerPath);
      var doc = JsonNode.Parse(content)!;
      var tasks = doc["tasks"]?.AsArray() ?? new JsonArray();

      if (tasks.Any(t => t?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true))
         throw new CommandException($"Task '{Name}' already exists. Use 'task update' to modify it.");

      var newTask = BuildTaskObject();

      tasks.Add(newTask);
      doc["tasks"] = tasks;

      await _fileSystem.File.WriteAllTextAsync(taskRunnerPath,
         doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
      $"Task '{Name}' added to task-runner.json".Log(ConsoleColor.Green);
   }

   private void RunInteractiveWizard()
   {
      AnsiConsole.Write(new Rule("[cyan]Add New Task[/]").LeftJustified());

      Name ??= AnsiConsole.Ask<string>("Task [green]name[/]:");
      Command ??= AnsiConsole.Ask<string>("Task [green]command[/]:");

      var argsInput = AnsiConsole.Ask<string>("Arguments [grey](space-separated, leave empty to skip)[/]:", "");
      if (!string.IsNullOrWhiteSpace(argsInput))
         Args = argsInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      Group = AnsiConsole.Ask<string>("Group [grey](leave empty to skip)[/]:", "");
      if (string.IsNullOrWhiteSpace(Group)) Group = null;

      Cwd = AnsiConsole.Ask<string>("Working directory [grey](leave empty to skip)[/]:", "");
      if (string.IsNullOrWhiteSpace(Cwd)) Cwd = null;

      Branch = AnsiConsole.Ask<string>("Branch pattern [grey](regex, leave empty to skip)[/]:", "");
      if (string.IsNullOrWhiteSpace(Branch)) Branch = null;

      var pathModeChoice = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
            .Title("Path mode:")
            .AddChoices("(none)", nameof(PathModes.Relative), nameof(PathModes.Absolute)));
      PathMode = pathModeChoice == "(none)" ? null : pathModeChoice;

      var outputChoice = AnsiConsole.Prompt(
         new SelectionPrompt<string>()
            .Title("Output mode:")
            .AddChoices("(none)", nameof(OutputTypes.Always), nameof(OutputTypes.Verbose), nameof(OutputTypes.Never)));
      Output = outputChoice == "(none)" ? null : outputChoice;

      var includeInput = AnsiConsole.Ask<string>("Include patterns [grey](space-separated, leave empty to skip)[/]:", "");
      if (!string.IsNullOrWhiteSpace(includeInput))
         Include = includeInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      var excludeInput = AnsiConsole.Ask<string>("Exclude patterns [grey](space-separated, leave empty to skip)[/]:", "");
      if (!string.IsNullOrWhiteSpace(excludeInput))
         Exclude = excludeInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
   }

   private JsonObject BuildTaskObject()
   {
      var task = new JsonObject { ["name"] = Name, ["command"] = Command };

      if (Args is { Count: > 0 })
      {
         var argsArray = new JsonArray();
         foreach (var arg in Args) argsArray.Add(JsonValue.Create(arg));
         task["args"] = argsArray;
      }

      if (!string.IsNullOrWhiteSpace(Group)) task["group"] = Group;
      if (!string.IsNullOrWhiteSpace(Cwd)) task["cwd"] = Cwd;
      if (!string.IsNullOrWhiteSpace(Branch)) task["branch"] = Branch;
      if (!string.IsNullOrWhiteSpace(PathMode)) task["pathMode"] = PathMode;
      if (!string.IsNullOrWhiteSpace(Output)) task["output"] = Output;

      if (Include is { Count: > 0 })
      {
         var arr = new JsonArray();
         foreach (var p in Include) arr.Add(JsonValue.Create(p));
         task["include"] = arr;
      }

      if (Exclude is { Count: > 0 })
      {
         var arr = new JsonArray();
         foreach (var p in Exclude) arr.Add(JsonValue.Create(p));
         task["exclude"] = arr;
      }

      return task;
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }
}

