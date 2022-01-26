using Husky.Services.Contracts;
using Husky.TaskRunner;

namespace Husky.Utils;

public static class Utils
{
   public static async Task<string> GetTaskCwdAsync(this IGit git, HuskyTask task)
   {
      string cwd;
      if (string.IsNullOrEmpty(task.Cwd))
         cwd = Path.GetFullPath(await git.GetGitPathAsync(), Environment.CurrentDirectory);
      else
         cwd = Path.IsPathFullyQualified(task.Cwd) ? task.Cwd : Path.GetFullPath(task.Cwd, Environment.CurrentDirectory);
      return cwd;
   }
}
