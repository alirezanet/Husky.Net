using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.TaskRunner;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase, IRunOption
{
   private readonly ICliWrap _cliWrap;
   private readonly IHuskyTaskLoader _taskLoader;
   private readonly IExecutableTaskFactory _executableTaskFactory;

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

   public RunCommand(ICliWrap cliWrap, IHuskyTaskLoader taskLoader, IExecutableTaskFactory executableTaskFactory)
   {
      _cliWrap = cliWrap;
      _taskLoader = taskLoader;
      _executableTaskFactory = executableTaskFactory;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole _)
   {
      var taskRunner = new TaskRunner.TaskRunner(this, _cliWrap, _taskLoader, _executableTaskFactory);
      await taskRunner.Run();
   }
}
