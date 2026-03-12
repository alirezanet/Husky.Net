using System.IO.Abstractions;
using CliFx.Attributes;
using Husky.Services.Contracts;

namespace Husky.Cli;

[Command("task ls", Description = "List all tasks in task-runner.json (alias: task list)")]
public class TaskLsCommand : TaskListCommand
{
   public TaskLsCommand(IGit git, IFileSystem fileSystem) : base(git, fileSystem) { }
}
