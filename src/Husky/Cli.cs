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


   private static string[] HandleHighPriorityArgs(string[] args)
   {
      if (args.Contains("--no-color"))
         Logger.Colors = false;

      if (args.Contains("--verbose") || args.Contains("-v"))
         Logger.Verbose = true;

      return args.Where(x => x != "--verbose" && x != "-v" && x != "--no-color").ToArray();
   }

   private static async ValueTask<int> RunCommand(string cmd, IReadOnlyList<string> args)
   {
      var ac = args.Count;
      return cmd switch
      {
         "--no-color" or "-v" or "--verbose" => 0,
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
   -v | --verbose      Show verbose output
   -c | --no-color     Disable color output

Commands:
   husky install [dir] (default: .husky)   Install Husky hooks
   husky uninstall                         Uninstall Husky hooks
   husky set <.husky/file> [cmd]           Set Husky hook (.husky/pre-push ""dotnet test"")
   husky add <.husky/file> [cmd]           Add Husky hook (.husky/pre-commit ""dotnet husky run"")
   husky run [--name] [--group]            Run task-runner.json tasks
   husky exec <.husky/csx/file.csx>        Execute a csharp script (.csx) file

-- learn more: {CliActions.DOCS_URL}
".Log();
      return 0;
   }
}
