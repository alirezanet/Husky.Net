using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("task rm", Description = "Remove a task from task-runner.json")]
public class TaskRemoveCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   [CommandOption("name", 'n', Description = "Task name to remove", IsRequired = true)]
   public string Name { get; set; } = default!;

   public TaskRemoveCommand(IGit git, IFileSystem fileSystem)
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

      var taskIndex = -1;
      for (var i = 0; i < tasks.Count; i++)
      {
         if (tasks[i]?["name"]?.GetValue<string>()?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true)
         {
            taskIndex = i;
            break;
         }
      }

      if (taskIndex == -1)
         throw new CommandException($"Task '{Name}' not found in task-runner.json");

      tasks.RemoveAt(taskIndex);
      await _fileSystem.File.WriteAllTextAsync(taskRunnerPath,
         doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
      $"Task '{Name}' removed from task-runner.json".Log(ConsoleColor.Green);
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }
}
