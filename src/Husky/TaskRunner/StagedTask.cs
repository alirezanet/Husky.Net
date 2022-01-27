using System.Text.RegularExpressions;
using CliFx.Exceptions;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.TaskRunner;

public class StagedTask : ExecutableTask
{
   private readonly IGit _git;

   public StagedTask(TaskInfo taskInfo, IGit git) : base(taskInfo)
   {
      TaskType = ExecutableTaskTypes.Staged;
      _git = git;
   }

   public override async Task<double> Execute(ICliWrap cli)
   {
      // Check if any partial staged files are present
      var diffNames = await _git.GetDiffNameOnlyAsync();
      var fileArgumentInfo = TaskInfo.ArgumentInfo.OfType<FileArgumentInfo>().ToList();
      var partialStagedFiles = fileArgumentInfo
          .IntersectBy(diffNames, q => q.RelativePath)
          .ToList();

      var hasAnyPartialStagedFiles = partialStagedFiles.Any();

      if (hasAnyPartialStagedFiles)
         return await PartialExecution(cli, partialStagedFiles);

      var executionTime = await base.Execute(cli);
      await ReStageFiles(partialStagedFiles);
      return executionTime;
   }

   private async Task<double> PartialExecution(
       ICliWrap cli,
       List<FileArgumentInfo> partialStagedFiles
   )
   {
      // create tmp folder
      var gitPath = await _git.GetGitDirRelativePathAsync();
      var tmp = Path.Combine(gitPath, "tmp");
      if (!Directory.Exists(tmp))
         Directory.CreateDirectory(tmp);

      var arguments = TaskInfo.Arguments.ToList();
      var tmpFiles = new List<DiffRecord>();

      var stagedRecord = await GetStagedRecord();
      foreach (var psf in partialStagedFiles)
      {
         var record = stagedRecord.First(q => q.src_path == psf.RelativePath);

         // first, we need to create a temporary file
         var tmpFile = Path.Combine(tmp, new FileInfo(psf.RelativePath).Name);

         if (psf.PathMode == PathModes.Absolute)
            tmpFile = Path.GetFullPath(tmpFile);

         var hash = record.dst_hash;
         {
            await using var output = File.Create(tmpFile);
            await (
                CliWrap.Cli.Wrap("git").WithArguments(new[] { "cat-file", "blob", hash }!)
                | output
            ).ExecuteAsync();
         }

         // insert the temporary file into the arguments
         var index = arguments.FindIndex(q => q == psf.Argument);
         arguments.Insert(index, tmpFile);
         tmpFiles.Add(record with { tmp_path = tmpFile });
      }

      // update arguments (add tmp files)
      TaskInfo.Arguments = arguments.ToArray();

      // execute the task to format all staged files and temporary files
      var executionTime = await base.Execute(cli);

      // stage temporary files
      foreach (var tf in tmpFiles)
      {
         // add formatted temp file to git database
         var result = await _git.ExecBufferedAsync($"hash-object -w {tf.tmp_path}");
         var newHash = result.StandardOutput.Trim();

         // check if the partial hash exists
         if (string.IsNullOrEmpty(newHash))
         {
            File.Delete(tf.tmp_path!);
            throw new CommandException(
                "Failed to hash temp file. Please check the partial staged files."
            );
         }

         if (newHash != tf.dst_hash)
         {
            $"Updating index entry for {tf.src_path}".LogVerbose();
            await _git.ExecAsync(
                $"update-index --cacheinfo {tf.dst_mode},{newHash},{tf.src_path}"
            );
         }
         else
         {
            $"file {tf.src_path} did not changed by formatters".LogVerbose();
         }

         // remove the temporary file
         File.Delete(tf.tmp_path!);
      }

      // re-staged staged files
      await ReStageFiles(partialStagedFiles);

      return executionTime;
   }

   private async Task ReStageFiles(List<FileArgumentInfo> partialStagedFiles)
   {
      var stagedFiles = TaskInfo.ArgumentInfo
          .OfType<FileArgumentInfo>()
          .Where(q => q.ArgumentTypes == ArgumentTypes.StagedFile)
          .Except(partialStagedFiles)
          .Select(q => q.RelativePath)
          .ToList();
      if (stagedFiles.Any())
      {
         $"Re-staging staged files...".LogVerbose();
         string.Join(Environment.NewLine, stagedFiles).LogVerbose();
         await _git.ExecAsync($"add {string.Join(" ", stagedFiles)}");
      }
   }

   private async Task<List<DiffRecord>> GetStagedRecord()
   {
      var diffStaged = await _git.GetDiffStagedRecord();
      var parsedDiff = diffStaged.Select(ParseDiff).AsQueryable();
      var stagedRecord = parsedDiff
          .Where(
              x =>
                  x.dst_mode != "120000" // symlinks
                  && TaskInfo.ArgumentInfo
                      .OfType<FileArgumentInfo>()
                      .Where(q => q.ArgumentTypes == ArgumentTypes.StagedFile)
                      .Select(q => q.RelativePath)
                      .Contains(x.src_path)
          )
          .ToList();
      return stagedRecord;
   }

   private DiffRecord ParseDiff(string diff)
   {
      // Format: src_mode dst_mode src_hash dst_hash status/score? src_path dst_path?
      var diff_pat = new Regex(
          @"^:(\d+) (\d+) ([a-f0-9]+) ([a-f0-9]+) ([A-Z])(\d+)?\t([^\t]+)(?:\t([^\t]+))?$"
      );
      var match = diff_pat.Match(diff);
      if (!match.Success)
         throw new CommandException("Failed to parse diff-index line:  + diff");

      return new DiffRecord(
          UnlessZeroed(match.Groups[1].Value),
          UnlessZeroed(match.Groups[2].Value),
          UnlessZeroed(match.Groups[3].Value),
          UnlessZeroed(match.Groups[4].Value),
          match.Groups[5].Value,
          int.TryParse(match.Groups[6].Value, out var score) ? score : null,
          match.Groups[7].Value,
          match.Groups[8].Value
      );
   }

   /// <summary>
   /// Returns the argument unless the argument is a string of zeroes, in which case
   /// returns `None`
   /// </summary>
   /// <param name="s"></param>
   private string? UnlessZeroed(string s)
   {
      var zeroed_pat = new Regex(@"^0+$");
      return zeroed_pat.IsMatch(s) ? null : s;
   }

   private record DiffRecord(
       string? src_mode,
       string? dst_mode,
       string? src_hash,
       string? dst_hash,
       string? status,
       int? score,
       string? src_path,
       string? dst_path,
       string? tmp_path = null
   );
}
