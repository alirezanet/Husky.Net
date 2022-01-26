using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public abstract class ExecutableTaskBase : IExecutableTask
{
   protected ExecutableTaskBase(ExecutableTaskTypes taskType)
   {
      TaskType = taskType;
   }
   public ExecutableTaskTypes TaskType { get; }
   public abstract Task<double> Execute(ICliWrap cli);
}
