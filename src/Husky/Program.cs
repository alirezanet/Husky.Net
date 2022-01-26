using System.IO.Abstractions;
using System.Text.RegularExpressions;
using CliFx;
using CliFx.Infrastructure;
using Husky.Cli;
using Husky.Services;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.TaskRunner;
using Microsoft.Extensions.DependencyInjection;

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

   // initialize DI
   var services = new ServiceCollection()
      .AddSingleton<IGit, Git>()
      .AddSingleton<IHuskyTaskLoader, HuskyTaskLoader>()
      .AddSingleton<IArgumentParser, ArgumentParser>()
      .AddTransient<IExecutableTaskFactory, ExecutableTaskFactory>()
      .AddTransient<IFileSystem, FileSystem>()
      .AddTransient<IXmlIO, XmlIO>()
      .AddTransient<ICliWrap, HuskyCliWrap>()
      .AddTransient<AddCommand>()
      .AddTransient<AttachCommand>()
      .AddTransient<ExecCommand>()
      .AddTransient<InstallCommand>()
      .AddTransient<RunCommand>()
      .AddTransient<SetCommand>()
      .AddTransient<UninstallCommand>();
   var serviceProvider = services.BuildServiceProvider();

   // initialize CLI
   exitCode = await new CliApplicationBuilder()
      .AddCommandsFromThisAssembly()
      .UseTypeActivator(serviceProvider.GetService)
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
