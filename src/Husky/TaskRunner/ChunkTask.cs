using System.Diagnostics;
using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public class ChunkTask : ExecutableTaskBase
{
   public IExecutableTask[] Chunks { get; }
   public ChunkTask(IExecutableTask[] executableTasks) : base(ExecutableTaskTypes.Chunked)
   {
      Chunks = executableTasks;
   }
   public override async Task<double> Execute(ICliWrap cli)
   {
      var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };
      var sw = Stopwatch.StartNew();
      await Parallel.ForEachAsync(Chunks, options, async (task, _) => await task.Execute(cli));
      sw.Stop();
      return sw.ElapsedMilliseconds;
   }
}
