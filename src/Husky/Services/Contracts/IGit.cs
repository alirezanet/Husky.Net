using CliWrap;
using CliWrap.Buffered;

namespace Husky.Services.Contracts;

public interface IGit
{
   Task<string[]> GetDiffNameOnlyAsync();
   Task<string[]> GetStagedFilesAsync();
   Task<string[]> GitFilesAsync();
   Task<string[]> GetLastCommitFilesAsync();
   Task<string> GetGitPathAsync();
   Task<string> GetGitDirRelativePathAsync();
   Task<string> GetCurrentBranchAsync();
   Task<string> GetHuskyPathAsync();

   /// <summary>
   /// Checks if a given path is a submodule.
   /// <see href="https://git-scm.com/docs/git-rev-parse#Documentation/git-rev-parse.txt---show-superproject-working-tree">relevant git documentation</see>
   /// </summary>
   /// <param name="path">path to check</param>
   /// <returns>true if submodule, false otherwise.</returns>
   Task<bool> IsSubmodule(string path);

   /// <summary>
   /// Returns the path to the `.git` directory
   /// </summary>
   /// <param name="path">Target path for the git command</param>
   /// <returns>Path to .git directory if found.</returns>
   Task<string> GetGitDirectory(string path);

   /// <summary>
   /// only file additions and modifications
   /// </summary>
   /// <returns></returns>
   Task<string[]> GetDiffStagedRecord();
   Task<CommandResult> ExecAsync(string args);
   Task<BufferedCommandResult> ExecBufferedAsync(string args);
}
