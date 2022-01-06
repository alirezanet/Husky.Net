using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Stdout;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase
{
   [CommandOption("name", 'n', Description = "Filter tasks by name")]
   public string? Name { get; set; }

   [CommandOption("group", 'g', Description = "Filter tasks by group")]
   public string? Group { get; set; }

   [CommandOption("args", 'a', Description = "Pass custom arguments to tasks")]
   public IReadOnlyList<string>? Arguments { get; set; }

   [CommandOption("no-color", 'c', Description = "Disable color output")]
   public bool NoColor
   {
      get => !Logger.Colors;
      set => Logger.Colors = !value;
   }

   [CommandOption("quiet", 'q', Description = "Disable [Husky] console output")]
   public bool Quiet
   {
      get => Logger.Quiet;
      set => Logger.Quiet = value;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var taskRunner = new TaskRunner.TaskRunner();
      await taskRunner.Run(this);
   }
}
