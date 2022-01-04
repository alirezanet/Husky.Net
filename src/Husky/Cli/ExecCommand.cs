using System.Collections.Immutable;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Stdout;
using Husky.TaskRunner;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Husky.Cli;

[Command("exec", Description = "Execute a csharp script (.csx) file")]
public class ExecCommand : CommandBase
{
   [CommandParameter(0, Description = "The script file to execute")]
   public string Path { get; set; } = default!;

   [CommandOption("args", 'a', Description = "Arguments to pass to the script")]
   public IList<string> Arguments { get; set; } = new List<string>();

   public override async ValueTask ExecuteAsync(IConsole console)
   {
      if (!File.Exists(Path))
         throw new CommandException($"can not find script file on '{Path}'");

      var code = await File.ReadAllTextAsync(Path);
      var workingDirectory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
      var opts = ScriptOptions.Default
         .WithSourceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, workingDirectory))
         .WithImports("System", "System.IO", "System.Collections.Generic", "System.Text", "System.Threading.Tasks");
      var script = CSharpScript.Create(code, opts, typeof(Globals));
      var compilation = script.GetCompilation();
      var diagnostics = compilation.GetDiagnostics();

      //check for warnings and errors
      if (diagnostics.Any())
      {
         foreach (var diagnostic in diagnostics)
         {
            switch (diagnostic.Severity)
            {
               case DiagnosticSeverity.Hidden:
                  diagnostic.GetMessage().LogVerbose();
                  break;
               case DiagnosticSeverity.Info:
                  diagnostic.GetMessage().Log(ConsoleColor.DarkBlue);
                  break;
               case DiagnosticSeverity.Warning:
                  diagnostic.GetMessage().Log(ConsoleColor.Yellow);
                  break;
               case DiagnosticSeverity.Error:
                  throw new CommandException(diagnostic.GetMessage() + "\nscript compilation failed");
               default:
                  throw new CommandException($"unknown diagnostic severity '{diagnostic.Severity}'");
            }
         }
      }

      var result = await script.RunAsync(new Globals() { Args = Arguments });
      if (result.Exception is null && result.ReturnValue is null or 0)
         return;

      if (result.ReturnValue is int i)
         throw new CommandException("script execution failed", i, false, result.Exception);

      throw new CommandException("script execution failed", innerException: result.Exception);
   }
}
