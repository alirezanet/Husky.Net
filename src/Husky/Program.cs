using Husky;
using H = Husky.Core;

var ln = args.Length;
if (ln is 0 or > 3)
   Help();

var cmd = args[0].ToLower();


void Help(int code = 0)
{
   @"Usage:
  husky install [dir] (default: .husky)
  husky uninstall
  husky set <project directory> <file> [cmd]
  husky set|add <file> [cmd]".Log(false);
#if !DEBUG
   Environment.Exit(code);
#endif
}

// CLI commands
Action hook = (cmd, ln) switch
{
   ("install", 1) => () => H.Install(),
   ("install", 3) => () => H.Install(args[1]),
   ("uninstall", 1) => H.Uninstall,
   ("add", 3) => () => H.Add(args[1], args[2]),
   ("set", 3) => () => H.Set(args[1], args[2]),
   _ => () =>
   {
      "Invalid command.".Log(false);
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

Console.Read();
