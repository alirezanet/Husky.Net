using System.Runtime.InteropServices;
using Husky.Logger;

namespace Husky;

public static class Cli
{
   private static void Exit(int code)
   {
#if DEBUG
      $"[Debug] Application Exited with code: {code}".Log(ConsoleColor.DarkMagenta);
#else
      Environment.Exit(code);
#endif
   }

   public static async ValueTask Start(string[] args)
   {
      HandleEvnArguments();

      if (!args.Any())
      {
         Help();
         Exit(0);
      }

      // remove high priority options
      args = HandleHighPriorityArgs(args);

      var cmd = args[0];
      try
      {
         // Run command
         var exitCode = await RunCommand(cmd, args);
         Exit(exitCode);
      }
      catch (Exception e)
      {
         // unhandled exceptions
         e.Message.LogErr();
         Exit(1);
      }
   }

   private static void HandleEvnArguments()
   {
      if (Environment.GetEnvironmentVariable("vt100") == "1")
      {
         try
         {
            // ENABLE_VIRTUAL_TERMINAL_PROCESSING
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
               Win32Console.Initialize();
               Logger.Logger.Vt100Colors = true;
            }
         }
         catch (Exception e)
         {
            e.Message.LogVerbose(ConsoleColor.Red);
         }
      }
      if (Environment.GetEnvironmentVariable("HUSKY_DEBUG") == "1")
      {
         Logger.Logger.Verbose = true;
      }
   }


   private static string[] HandleHighPriorityArgs(string[] args)
   {
      if (args.Contains("--no-color"))
         Logger.Logger.Colors = false;

      if (args.Contains("--verbose") || args.Contains("-v"))
         Logger.Logger.Verbose = true;

      if (args.Contains("--quiet") || args.Contains("-q"))
         Logger.Logger.Quiet = true;

      var exclude = new[] { "--no-color", "--verbose", "--quiet", "-v", "-q" };
      return args.Where(x => !exclude.Contains(x)).ToArray();
   }

   private static async ValueTask<int> RunCommand(string cmd, IReadOnlyList<string> args)
   {
      var ac = args.Count;
      return cmd switch
      {
         "--help" or "-h" or "-?" => Help(),
         "--version" or "-V" => CliActions.Version(),
         "install" when ac > 1 && !args[1].StartsWith("-") => await CliActions.Install(args[1]),
         "install" => await CliActions.Install(),
         "uninstall" => await CliActions.Uninstall(),
         "set" when ac == 2 => await CliActions.Set(args[1], "dotnet husky run"),
         "add" when ac == 2 => await CliActions.Add(args[1], "dotnet husky run"),
         "set" when ac >= 3 => await CliActions.Set(args[1], args[2]),
         "add" when ac >= 3 => await CliActions.Add(args[1], args[2]),
         "run" when ac is 1 => await CliActions.Run(),
         "run" => await CliActions.Run(args.Skip(1).ToArray()),
         "exec" when ac == 2 => await CliActions.Exec(args[1], Array.Empty<string>()),
         "exec" when ac > 2 => await CliActions.Exec(args[1], args.Skip(2).ToArray()),
         _ => InvalidCommand()
      };
   }

   private static int InvalidCommand()
   {
      "Invalid command. try 'husky --help'".LogErr();
      return 1;
   }

   private static int Help()
   {
      $@"Usage: husky [options] [command]

Options:
   -h | --help|-?      Show help information
   -V | --version      Show version information
   -v | --verbose      Show verbose output (default: false)
   -c | --no-color     Disable color output (default: false)
   -q | --quiet        Disable [Husky] console output (default: false)

Commands:
   husky install [dir] (default: .husky)    Install Husky hooks
   husky uninstall                          Uninstall Husky hooks
   husky set <.husky/file> [cmd]            Set Husky hook (.husky/pre-push ""dotnet test"")
   husky add <.husky/file> [cmd]            Add Husky hook (.husky/pre-commit ""dotnet husky run"")
   husky run [--name] [--group] [--args]    Run task-runner.json tasks
   husky exec <.husky/csx/file.csx> [args]  Execute a csharp script (.csx) file

-- learn more: {CliActions.DOCS_URL}
".Log();
      return 0;
   }
}
