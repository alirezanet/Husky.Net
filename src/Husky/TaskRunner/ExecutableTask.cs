using CliFx.Exceptions;
using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public partial class ExecutableTask : ExecutableTaskBase
{
   protected TaskInfo TaskInfo { get; }

   public ExecutableTask(TaskInfo taskInfo) : base(ExecutableTaskTypes.Normal)
   {
      TaskInfo = taskInfo;
   }

   public override async Task<double> Execute(ICliWrap cli)
   {
      var result = await cli.RunCommandAsync(
          TaskInfo.Command,
          TaskInfo.Arguments,
          TaskInfo.WorkingDirectory,
          TaskInfo.OutputType
      );
      if (result.ExitCode != 0)
         throw new CommandException(
             $"\n  ❌ Task '{TaskInfo.Name}' failed in {result.RunTime.TotalMilliseconds:n0}ms\n"
         );

      return result.RunTime.TotalMilliseconds;
   }
}
