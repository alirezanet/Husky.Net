using System.Diagnostics;
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

#if TEST
WaitForDebuggerIfNeeded();
#endif

#if DEBUG
"Starting development mode ... ".Log(ConsoleColor.DarkGray);
while (true)
{
   LoggerEx.logger = new Logger(new SystemConsole());

   "\nEnter your husky commands: ".Log();
   var cmd = Console.ReadLine();
   if (string.IsNullOrEmpty(cmd)) continue;
   // simulating args
   args = Regex.Matches(cmd!, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value.StartsWith("\"") ? m.Value.Replace("\"", "") : m.Value).ToArray();
#endif

   // initialize CLI
   exitCode = await new CliApplicationBuilder()
      .AddCommandsFromThisAssembly()
      .UseTypeActivator(BuildServiceProvider().GetService!)
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

ServiceProvider BuildServiceProvider()
{
   var services = new ServiceCollection()

      // Services
      .AddSingleton<IGit, Git>()
      .AddSingleton<IHuskyTaskLoader, HuskyTaskLoader>()
      .AddSingleton<IArgumentParser, ArgumentParser>()
      .AddTransient<IExecutableTaskFactory, ExecutableTaskFactory>()
      .AddTransient<IFileSystem, FileSystem>()
      .AddTransient<IXmlIO, XmlIO>()
      .AddTransient<IAssembly, AssemblyProxy>()
      .AddTransient<ICliWrap, HuskyCliWrap>()

      // Commands
      .AddTransient<AddCommand>()
      .AddTransient<AttachCommand>()
      .AddTransient<ExecCommand>()
      .AddTransient<InstallCommand>()
      .AddTransient<RunCommand>()
      .AddTransient<SetCommand>()
      .AddTransient<UninstallCommand>()
      .AddTransient<CleanupCommand>();
   return services.BuildServiceProvider();
}

#if TEST
        static void WaitForDebuggerIfNeeded()
        {
           if (Environment.GetEnvironmentVariable("HUSKY_INTEGRATION_TEST") != "1") return;
           Console.WriteLine("Waiting for debugger to attach...");

           while (!Debugger.IsAttached)
           {
              Thread.Sleep(100);
           }

           Console.WriteLine("Debugger attached.");
        }
#endif
