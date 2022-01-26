using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CliFx.Exceptions;
using Husky.Services.Contracts;
using Microsoft.Extensions.Configuration;

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
      try
      {
         var gitPath = await _git.GetGitPathAsync();
         var huskyPath = await _git.GetHuskyPathAsync();
         Tasks = new List<HuskyTask>();
         var dir = Path.Combine(gitPath, huskyPath, "task-runner.json");
         var config = new ConfigurationBuilder()
            .AddJsonFile(dir)
            .Build();
         config.GetSection("tasks").Bind(Tasks);
         OverrideWindowsSpecifics();
      }
      catch (FileNotFoundException e)
      {
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

      foreach (var task in Tasks.Where(q => q.Windows != null))
      {
         if (task.Windows == null) continue;
         if (task.Windows.Cwd != null)
            task.Cwd = task.Windows.Cwd;
         if (task.Windows.Args != null)
            task.Args = task.Windows.Args;
         if (task.Windows.Command != null)
            task.Command = task.Windows.Command;
         if (task.Windows.Group != null)
            task.Group = task.Windows.Group;
         if (task.Windows.Name != null)
            task.Name = task.Windows.Name;
         if (task.Windows.Exclude != null)
            task.Exclude = task.Windows.Exclude;
         if (task.Windows.Include != null)
            task.Include = task.Windows.Include;
         if (task.Windows.Output != null)
            task.Output = task.Windows.Output;
         if (task.Branch != null)
            task.Branch = task.Windows.Branch;
         if (task.Windows.PathMode != null)
            task.PathMode = task.Windows.PathMode;
      }
   }
}
