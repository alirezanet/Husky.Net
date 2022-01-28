using Husky.Stdout;

namespace Husky.TaskRunner;

public class TaskRunner
{
   private readonly IRunOption _options;
   private readonly IExecutableTaskFactory _factory;
   private readonly IHuskyTaskLoader _taskLoader;

   public TaskRunner(IHuskyTaskLoader taskLoader, IExecutableTaskFactory factory, IRunOption options)
   {
      _options = options;
      _taskLoader = taskLoader;
      _factory = factory;
   }

   public async ValueTask Run()
   {
      "🚀 Loading tasks ...".Husky();

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

         if (executableTask is null)
            continue;

         $"⌛ Executing task '{task.Name}' ...".Husky();
         var executionTime = await executableTask.Execute();

         $" ✔ Successfully executed in {executionTime:n0}ms".Husky(ConsoleColor.DarkGreen);
      }

      LoggerEx.Hr();
   }
}
