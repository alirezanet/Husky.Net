using System.Text.RegularExpressions;
using Husky;

#if DEBUG
"Starting development mode ... ".Log(ConsoleColor.DarkGray);
while (true)
{
   Logger.Colors = true;
   Logger.Verbose = false;

   "\nEnter your husky commands: ".Log();
   var cmd = Console.ReadLine();
   // simulating args
   args = Regex.Matches(cmd!, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value.StartsWith("\"") ? m.Value.Replace("\"", "") : m.Value).ToArray();
#endif

   // this is the real entry point
   await Cli.Start(args);

#if DEBUG
   "\nPress [Enter] to continue, [ESC] to exit ...".Log();
   var keyInfo = Console.ReadKey();
   if (keyInfo.Key == ConsoleKey.Escape)
      break;
   if (keyInfo.Key != ConsoleKey.Enter)
      Console.Clear();
}
#endif
