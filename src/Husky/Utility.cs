using CliWrap;
using CliWrap.Buffered;

namespace Husky;

public static class Utility
{
   public static async Task<BufferedCommandResult> ExecBufferedAsync(string fileName, string args)
   {
      try
      {
         var result = await CliWrap.Cli.Wrap(fileName)
            .WithArguments(args)
            .ExecuteBufferedAsync();
         return result;
      }
      catch (Exception)
      {
         $"failed to execute command '{fileName}'".LogErr();
         throw;
      }
   }

   public static async Task<CommandResult> ExecAsync(string fileName, string args)
   {
      try
      {
         var result = await CliWrap.Cli.Wrap(fileName)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(q => q.Log()))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(q => q.LogErr()))
            .ExecuteAsync();
         return result;
      }
      catch (Exception)
      {
         $"failed to execute command '{fileName}'".LogErr();
         throw;
      }
   }

   public static async Task<CommandResult> ExecAsync(string fileName, IEnumerable<string> args, string? cwd = null)
   {
      try
      {
         var ps = CliWrap.Cli.Wrap(fileName)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(q => q.logVerbose(ConsoleColor.DarkGray)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(q => q.logVerbose(ConsoleColor.DarkRed)));

         if (!string.IsNullOrEmpty(cwd))
            ps.WithWorkingDirectory(cwd);

         return await ps.ExecuteAsync();
      }
      catch (Exception)
      {
         $"failed to execute command '{fileName}'".LogErr();
         throw;
      }
   }
}
