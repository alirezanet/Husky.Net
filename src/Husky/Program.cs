using Husky;
using H = Husky.Core;

var ln = args.Length;
if (ln is 0 or > 3)
   Help();
var cmd = args[0].ToLower();

int Help(int code = 0)
{
   $@"Usage: husky [options] [command]

Options:
   -v|--version      Show version information
   -h|--help|-?      Show help information

Commands:
   husky install [dir] (default: .husky)     Install Husky hooks
   husky uninstall                           Uninstall Husky hooks
   husky set <.husky/file> [cmd]             Set Husky hook (.husky/pre-commit ""dotnet test"")
   husky add <.husky/file> [cmd]             Add Husky hook (.husky/pre-commit ""dotnet test"")
   husky run [--group] [--label]             Run defined tasks

-- learn more: {H.DOCS_URL}
".Log();
   Environment.Exit(code);
   return 0;
}

// CLI commands
Func<int> hook = (cmd, ln) switch
{
   ("--help" or "-h" or "-?", _) => () => Help(),
   ("--version" or "-v" , _) => H.Version,
   ("install", 1) => () => H.Install(),
   ("install", 2) => () => H.Install(args[1]),
   ("uninstall", 1) => H.Uninstall,
   ("add", 3) => () => H.Add(args[1], args[2]),
   ("set", 3) => () => H.Set(args[1], args[2]),
   ("staged", _) => H.Staged,
   _ => () =>
   {
      "Invalid command.".LogErr();
      Help(2);
      return 13;
   }
};

try
{
   // Run command
   var code = hook();
   Environment.Exit(code);
}
catch (Exception e)
{
   e.Message.LogErr();
   Environment.Exit(13);
}

