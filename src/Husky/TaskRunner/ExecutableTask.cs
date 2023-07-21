using CliFx.Exceptions;
using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public partial class ExecutableTask : ExecutableTaskBase
{
   protected TaskInfo TaskInfo { get; }

   public ExecutableTask(ICliWrap cliWrap, TaskInfo taskInfo) : base(cliWrap, ExecutableTaskTypes.Normal)
   {
      TaskInfo = taskInfo;
   }

   public override async Task<double> Execute()
   {
      var result = await _cliWrap.RunCommandAsync(
          TaskInfo.Command,
          TaskInfo.Arguments,
          TaskInfo.WorkingDirectory,
          TaskInfo.OutputType
      );
      if (result.ExitCode != 0 && !TaskInfo.IgnoreValidateCommandResult)
         throw new CommandException(
             $"\n  ❌ Task '{TaskInfo.Name}' failed in {result.RunTime.TotalMilliseconds:n0}ms\n"
         );

      return result.RunTime.TotalMilliseconds;
   }
}
