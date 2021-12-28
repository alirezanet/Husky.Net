using CliWrap;
using CliWrap.Buffered;

namespace Husky;

public class Git
{
   private Lazy<Task<string>> _gitPath { get; set; }
   private Lazy<Task<string>> _currentBranch { get; set; }
   private Lazy<Task<string>> _huskyPath { get; set; }
   private Lazy<Task<string[]>> _stagedFiles { get; set; }
   private Lazy<Task<string[]>> _lastCommitFiles { get; set; }

   public Task<string[]> StagedFiles => _stagedFiles.Value;
   public Task<string[]> LastCommitFiles => _lastCommitFiles.Value;
   public Task<string> GitPath => _gitPath.Value;
   public Task<string> CurrentBranch => _currentBranch.Value;
   public Task<string> HuskyPath => _huskyPath.Value;

   public Git()
   {
      _gitPath = new Lazy<Task<string>>(GetGitPath);
      _huskyPath = new Lazy<Task<string>>(GetHuskyPath);
      _stagedFiles = new Lazy<Task<string[]>>(GetStagedFiles);
      _lastCommitFiles = new Lazy<Task<string[]>>(GetLastCommitFiles);
      _currentBranch = new Lazy<Task<string>>(GetCurrentBranch);
   }

   private static async Task<string> GetCurrentBranch()
   {
      try
      {
         var result = await ExecBufferedAsync("branch --show-current");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim();
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         "Could not find git path".LogErr();
         throw;
      }
   }

   public static async Task<CommandResult> ExecAsync(string args)
   {
      return await Utility.ExecDirectAsync("git", args);
   }

   public static async Task<BufferedCommandResult> ExecBufferedAsync(string args)
   {
      return await Utility.ExecBufferedAsync("git", args);
   }

   private static async Task<string> GetHuskyPath()
   {
      try
      {
         var result = await ExecBufferedAsync("config --get core.hooksPath");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim();
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         "Could not find Husky path".LogErr();
         throw;
      }
   }

   private static async Task<string> GetGitPath()
   {
      try
      {
         var result = await ExecBufferedAsync("rev-parse --show-toplevel");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim();
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         "Could not find git path".LogErr();
         throw;
      }
   }

   private static async Task<string[]> GetLastCommitFiles()
   {
      try
      {
         var result = await ExecBufferedAsync("diff --diff-filter=d --name-only HEAD^");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim().Split('\n');
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         "Could not find the last commit files".LogErr();
         throw;
      }
   }

   private static async Task<string[]> GetStagedFiles()
   {
      try
      {
         var result = await ExecBufferedAsync("diff --diff-filter=d --name-only --staged");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim().Split('\n');
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         "Could not find the staged files".LogErr();
         throw;
      }
   }
}
