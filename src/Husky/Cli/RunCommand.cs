using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;
using Husky.TaskRunner;
using Husky.Utils;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase, IRunOption
{
   [CommandOption("quiet", 'q', Description = "Disable [Husky] console output")]
   public bool Quiet
   {
      get => LoggerEx.logger.HuskyQuiet;
      set => LoggerEx.logger.HuskyQuiet = value;
   }

   [CommandOption("name", 'n', Description = "Filter tasks by name")]
   public string? Name { get; set; }

   [CommandOption("group", 'g', Description = "Filter tasks by group")]
   public string? Group { get; set; }

   [CommandOption("args", 'a', Description = "Pass custom arguments to tasks")]
   public IReadOnlyList<string>? Arguments { get; set; }

   protected override async ValueTask SafeExecuteAsync(IConsole _)
   {
      var git = new Git();
      var taskRunner = new TaskRunner.TaskRunner(git, this);
      await taskRunner.Run();
   }
}
