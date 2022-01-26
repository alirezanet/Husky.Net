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
   /// only file additions and modifications
   /// </summary>
   /// <returns></returns>
   Task<string[]> GetDiffStagedRecord();
   Task<CommandResult> ExecAsync(string args);
   Task<BufferedCommandResult> ExecBufferedAsync(string args);
}
