using System.IO.Abstractions;
using System.Text.RegularExpressions;
using CliFx.Exceptions;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;

// Needed for .Net5
// ReSharper disable once RedundantUsingDirective
using Husky.Utils.Dotnet;


namespace Husky.TaskRunner;

public class StagedTask : ExecutableTask
{
   private readonly IGit _git;
   private readonly IFileSystem _fileSystem;

   public StagedTask(ICliWrap cliWrap, IGit git, IFileSystem fileSystem, TaskInfo taskInfo) : base(cliWrap, taskInfo)
   {
      TaskType = ExecutableTaskTypes.Staged;
      _git = git;
      _fileSystem = fileSystem;
   }

   public override async Task<double> Execute()
   {

      if (TaskInfo.NoPartial) return await NormalExecution();

      // Check if any partial staged files are present
      var diffNames = await _git.GetDiffNameOnlyAsync();
      var fileArgumentInfo = TaskInfo.ArgumentInfo.OfType<FileArgumentInfo>().ToList();
      var partialStagedFiles = fileArgumentInfo
         .IntersectBy(diffNames, q => q.RelativePath)
         .ToList();

      var hasAnyPartialStagedFiles = partialStagedFiles.Any();

      if (hasAnyPartialStagedFiles)
         return await PartialExecution(partialStagedFiles);

      var executionTime = await base.Execute();
      await ReStageFiles(partialStagedFiles);
      return executionTime;

   }

   private async Task<double> NormalExecution()
   {
      var executionTime = await base.Execute();
      // in staged mode, we should update the git index
      try
      {
         await _git.ExecAsync("update-index -g");
      }
      catch (Exception)
      {
         // Silently ignore the error if happens, we don't want to break the execution
         "⚠️ Can not update git index".Husky(ConsoleColor.Yellow);
      }

      return executionTime;
   }

   private async Task<double> PartialExecution(List<FileArgumentInfo> partialStagedFiles)
   {
      var arguments = TaskInfo.Arguments.ToList();
      var tmpFiles = new List<DiffRecord>();

      var stagedRecord = await GetStagedRecord();

      using var scope = new DisposableScope(() => "Cleaning up ...".LogVerbose());

      foreach (var psf in partialStagedFiles)
      {
         // create temp file
         var tmpFile = scope.Using(new TemporaryFile(_fileSystem, psf));

         // find the diff record for the file
         var record = stagedRecord.First(q => q.src_path == psf.RelativePath);
         var hash = record.dst_hash;

         // add staged content to temp file
         {
            await using var output = _fileSystem.File.Create(tmpFile);
            await (_cliWrap.Wrap("git").WithArguments(new[] { "cat-file", "blob", hash }!) | output).ExecuteAsync();
         }

         // insert the temporary file into the arguments
         var index = arguments.FindIndex(q => q == psf.Argument);
         arguments.Insert(index, tmpFile);

         // keep track of the temporary files and diff records
         tmpFiles.Add(record with { tmp_path = tmpFile });
      }

      // update arguments (add tmp files)
      TaskInfo.Arguments = arguments.ToArray();

      // execute the task to format all staged files and temporary files
      var executionTime = await base.Execute();

      // stage temporary files
      foreach (var tf in tmpFiles)
      {
         // add formatted temp file to git database
         var result = await _git.ExecBufferedAsync($"hash-object -w {tf.tmp_path}");
         var newHash = result.StandardOutput.Trim();

         // check if the partial hash exists
         if (string.IsNullOrEmpty(newHash))
         {
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
      }

      // re-staged staged files
      await ReStageFiles(partialStagedFiles);

      return executionTime;
   }

   private async Task ReStageFiles(IEnumerable<FileArgumentInfo> partialStagedFiles)
   {
      var stagedFiles = TaskInfo.ArgumentInfo
         .OfType<FileArgumentInfo>()
         .Where(q => q.ArgumentTypes == ArgumentTypes.StagedFile)
         .Except(partialStagedFiles)
         .Select(q => q.RelativePath)
         .ToList();
      if (stagedFiles.Any())
      {
         "Re-staging staged files...".LogVerbose();
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

   private static DiffRecord ParseDiff(string diff)
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
   private static string? UnlessZeroed(string s)
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
      TemporaryFile? tmp_path = null
   );
}
