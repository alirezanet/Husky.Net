using System.Reflection;
using Husky;
using H = Husky.Core;

var ln = args.Length;
if (ln is 0 or > 3)
   Help();
var cmd = args[0].ToLower();

void Help(int code = 0)
{
   $@"Usage: husky [options] [command]

Options:
   -v|--version      Show version information
   -h|--help|-?      Show help information

Commands:
   husky install [dir] (default: .husky)
   husky uninstall
   husky set <.husky/file> [cmd]
   husky add <.husky/file> [cmd]

-- learn more: {H.DOCS_URL}
".Log();
   Environment.Exit(code);
}

// CLI commands
Action hook = (cmd, ln) switch
{
   ("--help" or "-h" or "-?", _) => () => Help(),
   ("--version" or "-v" , _) => H.Version,
   ("install", 1) => () => H.Install(),
   ("install", 2) => () => H.Install(args[1]),
   ("uninstall", 1) => H.Uninstall,
   ("add", 3) => () => H.Add(args[1], args[2]),
   ("set", 3) => () => H.Set(args[1], args[2]),
   _ => () =>
   {
      "Invalid command.".LogErr();
      Help(2);
   }
};

try
{
   // Run command
   hook();
}
catch (Exception e)
{
   e.Message.LogErr();
}
