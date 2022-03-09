using CliFx.Exceptions;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;

namespace Husky.Services;

public class Git : IGit
{
   private readonly ICliWrap _cliWrap;
   private readonly AsyncLazy<string> _currentBranch;
   private readonly AsyncLazy<string> _gitDirRelativePath;
   private readonly AsyncLazy<string[]> _GitFiles;
   private readonly AsyncLazy<string> _gitPath;
   private readonly AsyncLazy<string> _huskyPath;
   private readonly AsyncLazy<string[]> _lastCommitFiles;
   private readonly AsyncLazy<string[]> _stagedFiles;

   public Git(ICliWrap cliWrap)
   {
      _cliWrap = cliWrap;
      _gitPath = new AsyncLazy<string>(GetGitPath);
      _huskyPath = new AsyncLazy<string>(GetHuskyPath);
      _stagedFiles = new AsyncLazy<string[]>(GetStagedFiles);
      _GitFiles = new AsyncLazy<string[]>(GetGitFiles);
      _lastCommitFiles = new AsyncLazy<string[]>(GetLastCommitFiles);
      _currentBranch = new AsyncLazy<string>(GetCurrentBranch);
      _gitDirRelativePath = new AsyncLazy<string>(GetGitDirRelativePath);
   }

   public async Task<string[]> GetDiffNameOnlyAsync()
   {
      try
      {
         var result = await ExecBufferedAsync("diff --name-only");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput
             .Trim()
             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("git diff failed", innerException: e);
      }
   }

   public async Task<string[]> GetStagedFilesAsync()
   {
      return await _stagedFiles;
   }

   public async Task<string[]> GitFilesAsync()
   {
      return await _GitFiles;
   }

   public async Task<string[]> GetLastCommitFilesAsync()
   {
      return await _lastCommitFiles;
   }

   public async Task<string> GetGitPathAsync()
   {
      return await _gitPath;
   }

   public async Task<string> GetGitDirRelativePathAsync()
   {
      return await _gitDirRelativePath;
   }

   public async Task<string> GetCurrentBranchAsync()
   {
      return await _currentBranch;
   }

   public async Task<string> GetHuskyPathAsync()
   {
      return await _huskyPath;
   }

   public async Task<string[]> GetDiffStagedRecord()
   {
      try
      {
         var result = await ExecBufferedAsync(
             "diff-index --cached --diff-filter=AM --no-renames HEAD"
         );
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput
             .Trim()
             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("Could not find the staged files", innerException: e);
      }
   }

   private async Task<string> GetGitDirRelativePath()
   {
      try
      {
         var result = await ExecBufferedAsync("rev-parse --path-format=relative --git-dir");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput.Trim();
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("Could not find git directory", innerException: e);
      }
   }

   private async Task<string> GetCurrentBranch()
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
         throw new CommandException("Could not find git path", innerException: e);
      }
   }

   public Task<CommandResult> ExecAsync(string args)
   {
      return _cliWrap.ExecDirectAsync("git", args);
   }

   public Task<BufferedCommandResult> ExecBufferedAsync(string args)
   {
      return _cliWrap.ExecBufferedAsync("git", args);
   }

   private async Task<string> GetHuskyPath()
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
         throw new CommandException("Could not find Husky path", innerException: e);
      }
   }

   private async Task<string> GetGitPath()
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
         throw new CommandException("Could not find git path", innerException: e);
      }
   }

   private async Task<string[]> GetLastCommitFiles()
   {
      try
      {
         var result = await ExecBufferedAsync("diff --diff-filter=d --name-only HEAD^");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput
             .Trim()
             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("Could not find the last commit files", innerException: e);
      }
   }

   private async Task<string[]> GetStagedFiles()
   {
      try
      {
         // '--diff-filter=AM', # select only file additions and modifications
         var result = await ExecBufferedAsync(
             "diff --staged --name-only --no-ext-diff --diff-filter=AM"
         );
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput
             .Trim()
             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("Could not find the staged files", innerException: e);
      }
   }

   private async Task<string[]> GetGitFiles()
   {
      try
      {
         var result = await ExecBufferedAsync("ls-files");
         if (result.ExitCode != 0)
            throw new Exception($"Exit code: {result.ExitCode}"); // break execution

         return result.StandardOutput
             .Trim()
             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch (Exception e)
      {
         e.Message.LogVerbose(ConsoleColor.DarkRed);
         throw new CommandException("Could not find the committed files", innerException: e);
      }
   }
}
