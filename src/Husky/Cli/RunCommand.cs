using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Stdout;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase
{
   [CommandOption("name", 'n', Description = "Task name")]
   public string? Name { get; set; }

   [CommandOption("group", 'g', Description = "Task group")]
   public string? Group { get; set; }

   [CommandOption("args", 'a', Description = "Task arguments")]
   public IReadOnlyList<string>? Arguments { get; set; }

   [CommandOption("no-color", 'c', Description = "Disable color output")]
   public bool NoColor
   {
      get => !Logger.Colors;
      set => Logger.Colors = !value;
   }

   [CommandOption("quite", 'q', Description = "Disable [Husky] console output")]
   public bool Quite
   {
      get => Logger.Quiet;
      set => Logger.Quiet = value;
   }

   public override async ValueTask ExecuteAsync(IConsole console)
   {
      var taskRunner = new TaskRunner.TaskRunner();
      var exitCode = await taskRunner.Run(this);
      if (exitCode != 0)
         throw new CommandException("", exitCode);
   }
}
