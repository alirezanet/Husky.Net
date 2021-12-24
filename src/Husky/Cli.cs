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
      var ln = args.Length;
      if (ln < 1)
      {
         Help();
         Exit(0);
      }

      // high priority options
      HandleHighPriorityArgs(ref args);

      var cmd = args[0];
      try
      {
         // Run command
         var exitCode = await RunCommand(cmd, ln, args);
         Exit(exitCode);
      }
      catch (Exception e)
      {
         // unhandled exceptions
         e.Message.LogErr();
         Exit(1);
      }
   }


   private static void HandleHighPriorityArgs(ref string[] args)
   {
      if (args.Contains("--no-color"))
      {
         args = args.Where(x => x != "--no-color").ToArray();
         Logger.Colors = false;
      }

      // ReSharper disable once InvertIf
      if (args.Contains("--verbose") || args.Contains("-v"))
      {
         args = args.Where(x => x != "--verbose" && x != "-v").ToArray();
         Logger.Verbose = true;
      }
   }

   private static async ValueTask<int> RunCommand(string cmd, int ln, IReadOnlyList<string> args)
   {
      return (cmd, ln) switch
      {
         ("--no-color" or "-v" or "--verbose", _) => 0,
         ("--help" or "-h" or "-?", _) => Help(),
         ("--version" or "-V", _) => CliActions.Version(),
         ("install", 1) => await CliActions.Install(),
         ("install", 2) => await CliActions.Install(args[1]),
         ("uninstall", 1) => await CliActions.Uninstall(),
         ("set", 3) => await CliActions.Set(args[1], args[2]),
         ("add", 3) => await CliActions.Add(args[1], args[2]),
         ("run", 1) => await CliActions.Run(),
         ("run", _) => await CliActions.Run(args.Skip(1).ToArray()),
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
   -v | --verbose      Show verbose output
   -c | --no-color     Disable color output

Commands:
   husky install [dir] (default: .husky)   Install Husky hooks
   husky uninstall                         Uninstall Husky hooks
   husky set <.husky/file> [cmd]           Set Husky hook (.husky/pre-push ""dotnet test"")
   husky add <.husky/file> [cmd]           Add Husky hook (.husky/pre-commit ""husky run"")
   husky run [--name] [--group]            Run task-runner.json tasks

-- learn more: {CliActions.DOCS_URL}
".Log();
      return 0;
   }
}
