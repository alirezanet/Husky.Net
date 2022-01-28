using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public abstract class ExecutableTaskBase : IExecutableTask
{
   protected readonly ICliWrap _cliWrap;

   protected ExecutableTaskBase(ICliWrap cliWrap, ExecutableTaskTypes taskType)
   {
      _cliWrap = cliWrap;
      TaskType = taskType;
   }
   public ExecutableTaskTypes TaskType { get; protected init; }
   public abstract Task<double> Execute();
}
