using System.Runtime.InteropServices;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky.TaskRunner;

public interface IExecutableTaskFactory
{
   Task<IExecutableTask?> CreateAsync(HuskyTask huskyTask, IRunOption options);
}

public class ExecutableTaskFactory : IExecutableTaskFactory
{
   private const double MAX_ARG_LENGTH = 8191;
   private readonly IArgumentParser _argumentParser;
   private readonly IServiceProvider _provider;
   private readonly IGit _git;

   public ExecutableTaskFactory(IServiceProvider provider, IGit git, IArgumentParser argumentParser)
   {
      _provider = provider;
      _git = git;
      _argumentParser = argumentParser;
   }

   public async Task<IExecutableTask?> CreateAsync(HuskyTask huskyTask, IRunOption options)
   {
      if (huskyTask.Command == null)
      {
         "💤 Skipped, no command found".Husky(ConsoleColor.Blue);
         return null;
      }

      huskyTask.Name ??= huskyTask.Command;

      var cwd = await _git.GetTaskCwdAsync(huskyTask);
      var argsInfo = await _argumentParser.ParseAsync(huskyTask, options.Arguments?.ToArray());

      if (await CheckIfWeShouldSkipTheTask(huskyTask, argsInfo))
         return null; // skip the task

      // check for chunk
      var totalCommandLength = argsInfo.Sum(q => q.Argument.Length) + huskyTask.Command.Length;
      if (
         RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
         && totalCommandLength > MAX_ARG_LENGTH
      )
         return CreateChunkTask(huskyTask, totalCommandLength, argsInfo, cwd, options.NoPartial);

      return CreateExecutableTask(
         new TaskInfo(
            huskyTask.Name,
            huskyTask.Command,
            argsInfo.Select(q => q.Argument).ToArray(),
            cwd,
            huskyTask.Output ?? OutputTypes.Always,
            argsInfo,
            options.NoPartial
         )
      );
   }

   private async Task<bool> CheckIfWeShouldSkipTheTask(HuskyTask huskyTask, ArgumentInfo[] argsInfo)
   {
      if (huskyTask is { FilteringRule: FilteringRules.Variable, Args: not null } && huskyTask.Args.Length > argsInfo.Length)
      {
         "💤 Skipped, no matched files".Husky(ConsoleColor.Blue);
         return true;
      }

      if (huskyTask.FilteringRule != FilteringRules.Staged) return false;

      var stagedFiles = (await _git.GetStagedFilesAsync())
         .Where(q => !string.IsNullOrWhiteSpace(q))
         .ToArray();
      if (stagedFiles.Length == 0)
      {
         "💤 Skipped, no staged files".Husky(ConsoleColor.Blue);
         return true;
      }

      var matcher = ArgumentParser.GetPatternMatcher(huskyTask);

      // get match staged files with glob
      var matches = matcher.Match(stagedFiles);
      if (matches.HasMatches) return false;
      "💤 Skipped, no staged matched files".Husky(ConsoleColor.Blue);
      return true;

   }

   private IExecutableTask CreateChunkTask(
      HuskyTask huskyTask,
      int totalCommandLength,
      ArgumentInfo[] args,
      string cwd,
      bool noPartial
   )
   {
      var chunks = GetChunks(totalCommandLength, args);
      var chunkLength = chunks.Count;
      var subTasks = new IExecutableTask[chunkLength];
      for (var i = 0; i < chunkLength; i++)
      {
         var argInfo = chunks.Dequeue();
         var chunkedArgs = argInfo.Select(w => w.Argument).ToArray();
         var taskInfo = new TaskInfo(
            huskyTask.Name!,
            huskyTask.Command!,
            chunkedArgs,
            cwd,
            huskyTask.Output ?? OutputTypes.Always,
            argInfo,
            noPartial
         );
         // staged-task
         subTasks[i] = CreateExecutableTask(taskInfo);
      }
      return ActivatorUtilities.CreateInstance<ChunkTask>(_provider, new object[] { subTasks });
   }

   private IExecutableTask CreateExecutableTask(TaskInfo taskInfo)
   {
      return taskInfo.ArgumentInfo.Any(q => q.ArgumentTypes == ArgumentTypes.StagedFile)
         ? ActivatorUtilities.CreateInstance<StagedTask>(_provider, taskInfo)
         : ActivatorUtilities.CreateInstance<ExecutableTask>(_provider, taskInfo);
   }

   private static Queue<ArgumentInfo[]> GetChunks(int totalCommandLength, ArgumentInfo[] args)
   {
      var chunkSize = Math.Ceiling(totalCommandLength / (MAX_ARG_LENGTH / 2));
      $"⚠️ The Maximum argument length '{MAX_ARG_LENGTH}' reached, splitting matched files into {chunkSize} chunks".Husky(
         ConsoleColor.Yellow
      );

      var totalFiles = args.Count(
         a => a.ArgumentTypes is ArgumentTypes.File or ArgumentTypes.StagedFile
      );
      var totalFilePerChunk = (int)Math.Ceiling(totalFiles / chunkSize);

      var chunks = new Queue<ArgumentInfo[]>((int)chunkSize);
      for (var i = 0; i < chunkSize; i++)
      {
         var chunk = new List<ArgumentInfo>();
         var fileCounter = 0;
         var skipSize = i == 0 ? 0 : i * totalFilePerChunk;
         foreach (var arg in args)
         {
            // add normal arguments
            if (arg.ArgumentTypes == ArgumentTypes.Static)
            {
               chunk.Add(arg);
               continue;
            }

            // if file already added to the chunk, skip it
            if (skipSize > 0)
            {
               skipSize -= 1;
               continue;
            }

            // add file to the chunk,
            // we should continue to the end
            // to support normal arguments after our file list if exists
            if (fileCounter >= totalFilePerChunk)
               continue;

            chunk.Add(arg);
            fileCounter += 1;
         }

         chunks.Enqueue(chunk.ToArray());
      }

      return chunks;
   }
}
