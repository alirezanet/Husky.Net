using System.Runtime.InteropServices;
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

   public static async Task<CommandResult> ExecDirectAsync(string fileName, string args)
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

   public static async Task<BufferedCommandResult> RunCommandAsync(string fileName, IEnumerable<string> args, string cwd,
      OutputTypes output = OutputTypes.Verbose)
   {
      var cmd = Path.IsPathFullyQualified(fileName) ? fileName : GetDefaultOsTerminal(fileName, ref args);
      var ps = CliWrap.Cli.Wrap(cmd)
         .WithWorkingDirectory(cwd)
         .WithArguments(args)
         .WithValidation(CommandResultValidation.None);

      if (!string.IsNullOrEmpty(cwd))
         ps = ps.WithWorkingDirectory(cwd);

      var result = await ps.ExecuteBufferedAsync();
      LogOutputs(output, result);
      return result;
   }

   private static void LogOutputs(OutputTypes output, BufferedCommandResult result)
   {
      if (result.ExitCode == 0)
      {
         switch (output)
         {
            case OutputTypes.Always:
               result.StandardOutput.Log();
               break;
            case OutputTypes.Error:
            case OutputTypes.Verbose:
               result.StandardOutput.LogVerbose();
               break;
            case OutputTypes.Never:
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, error, never, verbose)");
         }
      }
      else
      {
         switch (output)
         {
            case OutputTypes.Always:
            case OutputTypes.Error:
               if (result.StandardOutput.Length > 0)
                  result.StandardOutput.LogErr();
               if (result.StandardError.Length > 0)
                  result.StandardError.LogErr();
               break;
            case OutputTypes.Verbose:
               if (result.StandardOutput.Length > 0)
                  result.StandardOutput.LogVerbose(ConsoleColor.DarkRed);
               if (result.StandardError.Length > 0)
                  result.StandardError.LogVerbose(ConsoleColor.DarkRed);
               break;
            case OutputTypes.Never:
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, error, never, verbose)");
         }
      }
   }

   private static string GetDefaultOsTerminal(string fileName, ref IEnumerable<string> args)
   {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
         args = new[] { "/c", fileName }.Concat(args).ToArray();
         return "cmd.exe";
      }

      // ReSharper disable once InvertIf
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
         // according to: https://stackoverflow.com/a/15262019/637142
         // thanks to this we will pass everything as one command
         var command = $"{fileName} {string.Join(" ", args)}".Replace("\"", "\"\"");
         args = new[] { "-c \"" + command + "\"" };
         return "/bin/bash";
      }

      return fileName;
   }
}
