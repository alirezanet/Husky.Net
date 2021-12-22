namespace Husky;

public static class Cli
{
   public static async ValueTask Start(string[] args)
   {
      var ln = args.Length;
      if (ln is 0)
         Help();

      // high priority options
      if (args.Contains("--no-color"))
         Logger.Colors = false;

      if (args.Contains("--verbose") || args.Contains("-v"))
         Logger.Verbose = true;

      var cmd = args[0].ToLower();
      try
      {
         // Run command
         var exitCode = await RunCommand(cmd, ln, args);
         Environment.Exit(exitCode);
      }
      catch (Exception e)
      {
         // unhandled exceptions
         e.Message.LogErr();
         Environment.Exit(1);
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
         ("run", _) => await CliActions.Run(), // TODO : support group and name options
         _ => InvalidCommand()
      };
   }

   private static int InvalidCommand()
   {
      "Invalid command.".LogErr();
      return 1;
   }

   private static int Help()
   {
      $@"Usage: husky [options] [command]

Options:
   -V|--version      Show version information
   -h|--help|-?      Show help information

Commands:
   husky install [dir] (default: .husky)   Install Husky hooks
   husky uninstall                         Uninstall Husky hooks
   husky set <.husky/file> [cmd]           Set Husky hook (.husky/pre-push ""dotnet test"")
   husky add <.husky/file> [cmd]           Add Husky hook (.husky/pre-commit ""husky run"")
   husky run [--name] [--group]            Run predefined Husky tasks

-- learn more: {CliActions.DOCS_URL}
".Log();
      return 0;
   }
}
