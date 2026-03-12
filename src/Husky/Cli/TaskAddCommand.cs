using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("task add", Description = "Add a task to task-runner.json")]
public class TaskAddCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandOption("name", 'n', Description = "Task name", IsRequired = true)]
   public string Name { get; set; } = default!;

   [CommandOption("command", 'c', Description = "Command to run", IsRequired = true)]
   public string Command { get; set; } = default!;

   [CommandOption("args", 'a', Description = "Arguments to pass to the command")]
   public IReadOnlyList<string>? Args { get; set; }

   [CommandOption("group", 'g', Description = "Task group")]
   public string? Group { get; set; }

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

      var content = await _fileSystem.File.ReadAllTextAsync(taskRunnerPath);
      var doc = JsonNode.Parse(content)!;
      var tasks = doc["tasks"]?.AsArray() ?? new JsonArray();

      if (tasks.Any(t => t?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true))
         throw new CommandException($"Task '{Name}' already exists. Use 'task update' to modify it.");

      var newTask = new JsonObject
      {
         ["name"] = Name,
         ["command"] = Command
      };

      if (Args is { Count: > 0 })
      {
         var argsArray = new JsonArray();
         foreach (var arg in Args)
            argsArray.Add(JsonValue.Create(arg));
         newTask["args"] = argsArray;
      }

      if (Group != null)
         newTask["group"] = Group;

      tasks.Add(newTask);
      doc["tasks"] = tasks;

      await _fileSystem.File.WriteAllTextAsync(taskRunnerPath,
         doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
      $"Task '{Name}' added to task-runner.json".Log(ConsoleColor.Green);
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }
}
