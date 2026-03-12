using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("task update", Description = "Update an existing task in task-runner.json")]
public class TaskUpdateCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandOption("name", 'n', Description = "Task name to update", IsRequired = true)]
   public string Name { get; set; } = default!;

   [CommandOption("command", 'c', Description = "New command to run")]
   public string? Command { get; set; }

   [CommandOption("args", 'a', Description = "New arguments to pass to the command")]
   public IReadOnlyList<string>? Args { get; set; }

   [CommandOption("group", 'g', Description = "New task group")]
   public string? Group { get; set; }

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

      if (tasks == null)
         throw new CommandException("No tasks found in task-runner.json");

      var task = tasks.FirstOrDefault(t =>
         t?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true);

      if (task == null)
         throw new CommandException($"Task '{Name}' not found in task-runner.json");

      var taskObj = task.AsObject();

      if (Command != null)
         taskObj["command"] = Command;

      if (Args != null)
      {
         var argsArray = new JsonArray();
         foreach (var arg in Args)
            argsArray.Add(JsonValue.Create(arg));
         taskObj["args"] = argsArray;
      }

      if (Group != null)
         taskObj["group"] = Group;

      await _fileSystem.File.WriteAllTextAsync(taskRunnerPath,
         doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
      $"Task '{Name}' updated in task-runner.json".Log(ConsoleColor.Green);
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }
}
