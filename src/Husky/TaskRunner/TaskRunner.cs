using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.TaskRunner;

public class TaskRunner
{
   private readonly IRunOption _options;
   private readonly ICliWrap _cliWrap;
   private readonly IHuskyTaskLoader _taskLoader;
   private readonly IExecutableTaskFactory _factory;

   public TaskRunner(IRunOption options, ICliWrap cliWrap, IHuskyTaskLoader taskLoader, IExecutableTaskFactory factory)
   {
      _options = options;
      _cliWrap = cliWrap;
      _taskLoader = taskLoader;
      _factory = factory;
   }

   public async ValueTask Run()
   {
      "🚀 Preparing tasks ...".Husky();

      // load the tasks
      await _taskLoader.LoadAsync();
      await _taskLoader.ApplyOptions(_options);

      if (_taskLoader.Tasks.Count == 0)
      {
         "💤 Skipped, no task found".Husky();
         return;
      }

      foreach (var task in _taskLoader.Tasks)
      {
         LoggerEx.Hr();

         // use command for task name
         if (string.IsNullOrEmpty(task.Name))
            task.Name = task.Command;

         $"⚡ Preparing task '{task.Name}'".Husky();

         var executableTask = await _factory.CreateAsync(task, _options.Arguments?.ToArray());
         if (executableTask is null) continue;

         if (executableTask.TaskType == ExecutableTaskTypes.Chunked)
         {
            var chunkTask = (ChunkTask)executableTask;
            $"⌛ Executing task '{task.Name}' in {chunkTask.Chunks.Length} chunks ...".Husky();
         }
         else
         {
            $"⌛ Executing task '{task.Name}' ...".Husky();
         }
         var executionTime = await executableTask.Execute(_cliWrap);

         $" ✔ Successfully executed in {executionTime:n0}ms".Husky(ConsoleColor.DarkGreen);
      }

      LoggerEx.Hr();
   }
}
