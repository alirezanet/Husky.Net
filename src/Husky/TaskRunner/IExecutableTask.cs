using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public interface IExecutableTask
{
   ExecutableTaskTypes TaskType { get; }
   Task<double> Execute(ICliWrap cli);
}
