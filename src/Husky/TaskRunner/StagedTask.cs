using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.TaskRunner;

public class StagedTask : ExecutableTask
{
   private readonly IGit _git;

   public StagedTask(TaskInfo taskInfo, IGit git) : base(taskInfo)
   {
      TaskType = ExecutableTaskTypes.Staged;
      _git = git;
   }
   public override async Task<double> Execute(ICliWrap cli)
   {
      // before
      var executionTime = await base.Execute(cli);
      // after

      // in staged mode, we should update the git index
      try
      {
         await _git.ExecAsync("update-index -g");
      }
      catch (Exception)
      {
         // Silently ignore the error if happens, we don't want to break the execution
         "⚠️ Can not update git index".Husky(ConsoleColor.Yellow);
      }

      return executionTime;
   }
}
