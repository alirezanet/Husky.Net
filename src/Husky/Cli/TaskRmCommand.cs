using System.IO.Abstractions;
using CliFx.Attributes;
using Husky.Services.Contracts;

namespace Husky.Cli;

[Command("task rm", Description = "Remove a task from task-runner.json (alias: task remove)")]
public class TaskRmCommand : TaskRemoveCommand
{
   public TaskRmCommand(IGit git, IFileSystem fileSystem) : base(git, fileSystem) { }
}
