using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CliFx.Exceptions;
using Husky.Services.Contracts;
using Husky.Stdout;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Husky.TaskRunner;

public interface IHuskyTaskLoader
{
   IList<HuskyTask> Tasks { get; }
   ValueTask ApplyOptions(IRunOption options);
   Task LoadAsync();
}

public class HuskyTaskLoader : IHuskyTaskLoader
{
   private readonly IGit _git;

   public HuskyTaskLoader(IGit git)
   {
      _git = git;
      Tasks = new List<HuskyTask>();
   }

   public IList<HuskyTask> Tasks { get; private set; }


   public async Task LoadAsync()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyPath = await _git.GetHuskyPathAsync();
      Tasks = new List<HuskyTask>();
      var dir = Path.Combine(gitPath, huskyPath);
      try
      {
         var config = new ConfigurationBuilder()
            .SetFileProvider(new PhysicalFileProvider(dir, ExclusionFilters.None))
            .AddJsonFile("task-runner.json")
            .Build();
         config.GetSection("tasks").Bind(Tasks);
         OverrideWindowsSpecifics();

         // set default filteringRule if not defined
         foreach (var task in Tasks)
            task.FilteringRule ??= FilteringRules.Variable;
      }
      catch (FileNotFoundException e)
      {
         $"task-runner.json path: '{dir}'".LogVerbose();
         throw new CommandException("Can not find task-runner.json, try 'husky install'", innerException: e);
      }
   }

   public async ValueTask ApplyOptions(IRunOption options)
   {
      var query = Tasks.AsQueryable();

      // filter by name
      if (options.Name != null)
         query = query.Where(q => q.Name != null && q.Name.Equals(options.Name, StringComparison.OrdinalIgnoreCase));

      // filter by group
      if (options.Group != null)
         query = query.Where(q => q.Group != null && q.Group.Equals(options.Group, StringComparison.OrdinalIgnoreCase));

      // filter by branch
      if (query.Any(q => !string.IsNullOrEmpty(q.Branch)))
      {
         var branch = await _git.GetCurrentBranchAsync();
         query = query.Where(q => string.IsNullOrEmpty(q.Branch) || Regex.IsMatch(branch, q.Branch));
      }

      Tasks = query.ToList();
   }

   private void OverrideWindowsSpecifics()
   {
      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

      var properties = typeof(HuskyTask).GetProperties();
      foreach (var task in Tasks.Where(q => q.Windows != null))
      {
         // replace Task.Windows to Task
         foreach (var prop in properties)
         {
            if (prop.Name == nameof(HuskyTask.Windows))
               continue;

            var value = prop.GetValue(task.Windows);
            if (value != null)
               prop.SetValue(task, value);
         }
      }
   }
}
