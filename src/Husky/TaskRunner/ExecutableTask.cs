using CliFx.Exceptions;
using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public class ExecutableTask : ExecutableTaskBase
{
   private readonly TaskInfo _taskInfo;

   public ExecutableTask(TaskInfo taskInfo) : base(ExecutableTaskTypes.Normal)
   {
      _taskInfo = taskInfo;
   }

   public override async Task<double> Execute(ICliWrap cli)
   {
      var result = await cli.RunCommandAsync(_taskInfo.Command, _taskInfo.Arguments, _taskInfo.WorkingDirectory, _taskInfo.OutputType);
      if (result.ExitCode != 0)
         throw new CommandException($"\n  ❌ Task '{_taskInfo.Name}' failed in {result.RunTime.TotalMilliseconds:n0}ms\n");

      return result.RunTime.TotalMilliseconds;
   }
}
