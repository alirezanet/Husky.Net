using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;
using Husky.Stdout;
using Husky.TaskRunner;

namespace Husky.Helpers;

public static class Utility
{
   internal const string HUSKY_FOLDER_NAME = ".husky";

   // Custom dir help TODO: change this url to short version of docs
   internal const string DOCS_URL = "https://github.com/alirezanet/husky.net";

   public static async Task<BufferedCommandResult> ExecBufferedAsync(string fileName, string args)
   {
      try
      {
         var fullPath = GetFullyQualifiedPath(fileName);
         var result = await CliWrap.Cli.Wrap(fullPath)
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

   public static async ValueTask SetExecutablePermission(params string[] files)
   {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
      var chmod = GetFullyQualifiedPath("chmod");
      var args = new[] { "+x" }.Concat(files);
      var ps = CliWrap.Cli.Wrap(chmod)
         .WithArguments(args)
         .WithStandardErrorPipe(PipeTarget.ToDelegate(q => q.LogVerbose(ConsoleColor.DarkRed)))
         .WithValidation(CommandResultValidation.None);
      var result = await ps.ExecuteAsync();
      if (result.ExitCode != 0)
         "failed to add executable permissions".Log(ConsoleColor.Yellow);
   }

   public static async Task<CommandResult> ExecDirectAsync(string fileName, string args)
   {
      var fullPath = GetFullyQualifiedPath(fileName);
      var result = await CliWrap.Cli.Wrap(fullPath)
         .WithArguments(args)
         .WithValidation(CommandResultValidation.None)
         .WithStandardOutputPipe(PipeTarget.ToDelegate(q => q.Log()))
         .WithStandardErrorPipe(PipeTarget.ToDelegate(q => q.LogErr()))
         .ExecuteAsync();
      return result;
   }

   public static async Task<CommandResult> RunCommandAsync(string fileName, IEnumerable<string> args, string cwd,
      OutputTypes output = OutputTypes.Verbose)
   {
      var fullPath = GetFullyQualifiedPath(fileName);
      var ps = CliWrap.Cli.Wrap(fullPath)
         .WithWorkingDirectory(cwd)
         .WithArguments(args)
         .WithValidation(CommandResultValidation.None)
         .WithStandardOutputPipe(PipeTarget.ToDelegate(q => LogStandardOutput(q, output)))
         .WithStandardErrorPipe(PipeTarget.ToDelegate(q => LogStandardError(q, output)));

      if (!string.IsNullOrEmpty(cwd))
         ps = ps.WithWorkingDirectory(cwd);

      return await ps.ExecuteAsync();
   }

   public static string GetFullyQualifiedPath(string fileName)
   {
      var fullPath = GetFullPath(fileName);
      return fullPath ?? fileName;
   }

   public static string? GetFullPath(string fileName)
   {
      if (File.Exists(fileName))
         return Path.GetFullPath(fileName);

      var envValues = Environment.GetEnvironmentVariable("PATH");
      if (envValues == null) return null;
      foreach (var path in envValues.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
      {
         var fullPath = Path.Combine(path, fileName);
         // we should look for .exe and .cmd files on windows
         if (!fileName.EndsWith(".exe") && !fileName.EndsWith(".cmd") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
         {
            if (File.Exists(fullPath + ".exe"))
               return fullPath + ".exe";
            if (File.Exists(fullPath + ".cmd"))
               return fullPath + ".cmd";
         }
         else if (File.Exists(fullPath))
         {
            return fullPath;
         }
      }

      return null;
   }

   private static void LogStandardOutput(string stdout, OutputTypes output)
   {
      switch (output)
      {
         case OutputTypes.Always:
            stdout.Log();
            break;
         case OutputTypes.Verbose:
            stdout.LogVerbose();
            break;
         case OutputTypes.Never:
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, never, verbose)");
      }
   }

   private static void LogStandardError(string stdout, OutputTypes output)
   {
      switch (output)
      {
         case OutputTypes.Always:
            stdout.LogErr();
            break;
         case OutputTypes.Verbose:
            stdout.LogVerbose(ConsoleColor.DarkRed);
            break;
         case OutputTypes.Never:
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(output), output, "Supported (always, never, verbose)");
      }
   }
}
