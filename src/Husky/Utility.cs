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

   public static async ValueTask<int> SetExecutablePermission(params string[] files)
   {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
      var chmod = GetFullyQualifiedPath("chmod");
      var args = new[] { "+x" }.Concat(files);
      var ps = CliWrap.Cli.Wrap(chmod)
         .WithArguments(args)
         .WithStandardErrorPipe(PipeTarget.ToDelegate(q => q.LogVerbose(ConsoleColor.DarkRed)))
         .WithValidation(CommandResultValidation.None);
      var result = await ps.ExecuteAsync();
      if (result.ExitCode != 0)
         "failed to add executable permissions".LogErr();
      return result.ExitCode;
   }

   public static async Task<CommandResult> ExecDirectAsync(string fileName, string args)
   {
      try
      {
         var fullPath = GetFullyQualifiedPath(fileName);
         var result = await CliWrap.Cli.Wrap(fullPath)
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
      foreach (var path in envValues.Split(Path.PathSeparator))
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

   /// <summary>
   /// Loads the configuration data from the command line args.
   /// </summary>
   public static IDictionary<string, string> ParseArgs(IEnumerable<string> args)
   {
      var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      using var enumerator = args.GetEnumerator();
      while (enumerator.MoveNext())
      {
         var currentArg = enumerator.Current;
         var keyStartIndex = 0;

         if (currentArg.StartsWith("--"))
         {
            keyStartIndex = 2;
         }
         else if (currentArg.StartsWith("-"))
         {
            keyStartIndex = 1;
         }
         else if (currentArg.StartsWith("/"))
         {
            // "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
            // So we do a conversion to simplify later processing
            currentArg = $"--{currentArg[1..]}";
            keyStartIndex = 2;
         }

         var separator = currentArg.IndexOf(' ');

         string value;
         string key;
         if (separator < 0)
         {
            switch (keyStartIndex)
            {
               // If there is neither equal sign nor prefix in current argument, it is an invalid format
               case 0:
               // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage so ignore it
               case 1:
                  // Ignore invalid formats
                  continue;
            }


            // Otherwise, use the switch name directly as a key
            key = currentArg[keyStartIndex..];

            if (!enumerator.MoveNext())
               // ignore missing values
               continue;

            value = enumerator.Current;
         }
         else
         {
            // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
            if (keyStartIndex == 1) throw new ArgumentException(currentArg);
            // Otherwise, use the switch name directly as a key

            key = currentArg[keyStartIndex..separator];

            value = currentArg[(separator + 1)..];
         }

         // Override value when key is duplicated. So we always have the last argument win.
         data[key] = value;
      }

      return data;
   }
}
