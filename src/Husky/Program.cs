using System.Text.RegularExpressions;
using CliFx;
using CliFx.Infrastructure;
using Husky.Stdout;

// initialize static testable logger
LoggerEx.logger = new Logger(new SystemConsole());

var exitCode = 0;

#if DEBUG
"Starting development mode ... ".Log(ConsoleColor.DarkGray);
while (true)
{
   LoggerEx.logger.Colors = true;
   LoggerEx.logger.Verbose = false;

   "\nEnter your husky commands: ".Log();
   var cmd = Console.ReadLine();
   if (string.IsNullOrEmpty(cmd)) continue;
   // simulating args
   args = Regex.Matches(cmd!, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value.StartsWith("\"") ? m.Value.Replace("\"", "") : m.Value).ToArray();
#endif


   exitCode = await new CliApplicationBuilder()
      .AddCommandsFromThisAssembly()
      .SetExecutableName("husky")
      .Build()
      .RunAsync(args);


#if DEBUG
   $"\nExited with code {exitCode}".Log();
   "\nPress [Enter] to continue, [ESC] to exit ...".Log();
   var keyInfo = Console.ReadKey();
   if (keyInfo.Key == ConsoleKey.Escape)
      break;
   if (keyInfo.Key != ConsoleKey.Enter)
      Console.Clear();
}
#endif

return exitCode;
