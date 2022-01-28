using System.Diagnostics;
using Husky.Services.Contracts;

namespace Husky.TaskRunner;

public class ChunkTask : ExecutableTaskBase
{
   public IExecutableTask[] Chunks { get; }

   public ChunkTask(ICliWrap cliWrap, IExecutableTask[] executableTasks) : base(cliWrap, ExecutableTaskTypes.Chunked)
   {
      Chunks = executableTasks;
   }

   public override async Task<double> Execute()
   {
      var options = new ParallelOptions
      {
         MaxDegreeOfParallelism = (int)Math.Log2(Chunks.Length)
      };
      var sw = Stopwatch.StartNew();
      await Parallel.ForEachAsync(Chunks, options, async (task, _) => await task.Execute());
      sw.Stop();
      return sw.ElapsedMilliseconds;
   }
}
