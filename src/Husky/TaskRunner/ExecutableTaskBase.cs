using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public abstract class ExecutableTaskBase : IExecutableTask
{
   protected ExecutableTaskBase(ExecutableTaskTypes taskType)
   {
      TaskType = taskType;
   }
   public ExecutableTaskTypes TaskType { get; protected init; }
   public abstract Task<double> Execute(ICliWrap cli);
}
