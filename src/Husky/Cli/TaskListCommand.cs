using System.IO.Abstractions;
using System.Text.Json.Nodes;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("task ls", Description = "List all tasks in task-runner.json")]
public class TaskListCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   public TaskListCommand(IGit git, IFileSystem fileSystem)
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
      var doc = JsonNode.Parse(content);
      var tasks = doc?["tasks"]?.AsArray();

      if (tasks == null || tasks.Count == 0)
      {
         "No tasks found in task-runner.json".Log(ConsoleColor.Yellow);
         return;
      }

      foreach (var task in tasks)
      {
         var name = task?["name"]?.GetValue<string>() ?? "(unnamed)";
         var command = task?["command"]?.GetValue<string>() ?? "(no command)";
         var args = task?["args"]?.AsArray();
         var group = task?["group"]?.GetValue<string>();

         var argsStr = args != null ? string.Join(" ", args.Select(a => a?.GetValue<string>())) : "";
         var groupStr = group != null ? $" [group: {group}]" : "";
         $"  - {name}: {command} {argsStr}{groupStr}".Log();
      }
   }

   internal async Task<string> GetTaskRunnerPath()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      return Path.Combine(gitPath, huskyPath, "task-runner.json");
   }
}
