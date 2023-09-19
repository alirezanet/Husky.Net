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
      var sw = Stopwatch.StartNew();
      foreach (var chunk in Chunks)
      {
         await chunk.Execute();
      }
      sw.Stop();
      return sw.ElapsedMilliseconds;
   }
}
