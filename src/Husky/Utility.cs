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

   public static async Task<CommandResult> ExecAsync(string fileName, IEnumerable<string> args, string? cwd = null,
      HuskyTask.Outputs output = HuskyTask.Outputs.Verbose)
   {
      try
      {
         var ps = CliWrap.Cli.Wrap(fileName)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(q =>
            {
               switch (output)
               {
                  case HuskyTask.Outputs.Verbose:
                     q.logVerbose();
                     break;
                  case HuskyTask.Outputs.Always:
                     q.Log();
                     break;
                  case HuskyTask.Outputs.Never:
                     break;
                  default:
                     throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, never, verbose)");
               }
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(q =>
            {
               switch (output)
               {
                  case HuskyTask.Outputs.Verbose:
                     q.logVerbose(ConsoleColor.DarkRed);
                     break;
                  case HuskyTask.Outputs.Always:
                     q.LogErr();
                     break;
                  case HuskyTask.Outputs.Never:
                     break;
                  default:
                     throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, never, verbose)");
               }
            }));

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

   public static bool IsValidateCwd(string cwd)
   {
      return Directory.Exists(Path.Combine(cwd, ".git"));
   }
}
