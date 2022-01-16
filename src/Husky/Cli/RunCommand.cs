using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.TaskRunner;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase, IRunOption
{
   private readonly IGit _git;
   private readonly ICliWrap _cliWrap;

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

   public RunCommand(IGit git, ICliWrap cliWrap)
   {
      _git = git;
      _cliWrap = cliWrap;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole _)
   {
      var taskRunner = new TaskRunner.TaskRunner(_git, this, _cliWrap);
      await taskRunner.Run();
   }
}
