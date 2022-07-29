using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;
using Husky.TaskRunner;
using Microsoft.Extensions.DependencyInjection;

namespace Husky.Cli;

[Command("run", Description = "Run task-runner.json tasks")]
public class RunCommand : CommandBase, IRunOption
{
   private readonly IServiceProvider _provider;

   [CommandOption("quiet", 'q', Description = "Disable [Husky] console output", EnvironmentVariable = "HUSKY_QUIET")]
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

   [CommandOption("no-partial", 'p', Description = "Disable partial commit and temporary file generation",
      EnvironmentVariable = "HUSKY_NOPARTIAL")]
   public bool NoPartial { get; set; }

   [CommandOption("no-unicode", 'u', Description = "Disable unicode characters", EnvironmentVariable = "HUSKY_NOUNICODE")]
   public bool NoUnicode
   {
      get => LoggerEx.logger.NoUnicode;
      set => LoggerEx.logger.NoUnicode = value;
   }

   public RunCommand(IServiceProvider provider)
   {
      _provider = provider;
   }

   protected override async ValueTask SafeExecuteAsync(IConsole _)
   {
      var taskRunner = ActivatorUtilities.CreateInstance<TaskRunner.TaskRunner>(_provider, this);
      await taskRunner.Run();
   }
}
