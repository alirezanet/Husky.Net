using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Security.Cryptography;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;
using Husky.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Husky.Cli;

[Command("exec", Description = "Execute a csharp script (.csx) file")]
public class ExecCommand : CommandBase
{
   private readonly IFileSystem _fileSystem;
   private readonly IGit _git;
   private readonly IAssembly _assembly;
   private const string EXTENSION = ".dll";

   public ExecCommand(IFileSystem fileSystem, IGit git, IAssembly assembly)
   {
      _fileSystem = fileSystem;
      _git = git;
      _assembly = assembly;
   }

   [CommandParameter(0, Description = "The script file to execute")]
   public string Path { get; set; } = default!;

   [CommandOption("args", 'a', Description = "Arguments to pass to the script")]
   public IList<string> Arguments { get; set; } = new List<string>();

   [CommandOption("no-cache", Description = "Disable caching")]
   public bool NoCache { get; set; } = false;

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      if (!_fileSystem.File.Exists(Path))
         throw new CommandException($"can not find script file on '{Path}'");

      if (NoCache)
      {
         await ExecuteScript(Path);
         return;
      }

      var (exist, compiledScriptPath) = await GetCachedScript(Path);
      if (!exist)
         await GenerateScriptCache(Path, compiledScriptPath);

      await ExecuteCachedScript(compiledScriptPath);
   }

   private async Task GenerateScriptCache(string scriptPath, string compiledScriptPath)
   {
      var (_, compilation) = await GetCSharpScriptCompilation(scriptPath);

      compilation.Emit(compiledScriptPath);
   }

   private async Task<(Script<object>, Compilation)> GetCSharpScriptCompilation(string scriptPath)
   {
      var code = await _fileSystem.File.ReadAllTextAsync(scriptPath);
      var workingDirectory = _fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetFullPath(scriptPath));
      var opts = ScriptOptions.Default
         .WithSourceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, workingDirectory))
         .WithImports("System", "System.IO", "System.Collections.Generic", "System.Text", "System.Threading.Tasks");
      var script = CSharpScript.Create(code, opts, typeof(Globals));
      var compilation = script.GetCompilation();

      var diagnostics = compilation.GetDiagnostics();

      //check for warnings and errors
      if (diagnostics.Any())
         foreach (var diagnostic in diagnostics)
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

      return (script, compilation);
   }

   internal async Task ExecuteCachedScript(string scriptPath)
   {
      if (!_fileSystem.Path.IsPathFullyQualified(scriptPath))
      {
         var gitPath = await _git.GetGitPathAsync();
         scriptPath = _fileSystem.Path.Combine(gitPath, scriptPath);
      }

      var assembly = _assembly.LoadFile(scriptPath);
      var type = assembly.GetType("Submission#0");
      if (type == null)
      {
         throw new CommandException("Unable to execute cached script. Submission not found.");
      }

      var factory = type.GetMethod("<Factory>");
      if (factory == null)
      {
         throw new CommandException("Unable to execute cached script. Factory not found.");
      }

      var submissionArray = new object[2];
      submissionArray[0] = new Globals { Args = Arguments };

      if (factory.Invoke(null, new object[] { submissionArray }) is Task<object> task)
         try
         {
            var result = await task;
            switch (result)
            {
               case null or 0:
                  return;
               case int i:
                  throw new CommandException("script execution failed", i);
               default:
                  throw new CommandException("script execution failed");
            }
         }
         catch (CommandException)
         {
            throw;
         }
         catch (Exception e)
         {
            var message = "Unable to execute cached script.";
            if (LoggerEx.logger.Verbose)
               message = $"{message} {e.Message}";
            throw new CommandException(message, innerException: e);
         }
   }

   internal async ValueTask ExecuteScript(string scriptPath)
   {
      var (script, _) = await GetCSharpScriptCompilation(scriptPath);

      var result = await script.RunAsync(new Globals { Args = Arguments });
      if (result.Exception is null && result.ReturnValue is null or 0)
         return;

      if (result.ReturnValue is int i)
         throw new CommandException("script execution failed", i, false, result.Exception);

      throw new CommandException("script execution failed", innerException: result.Exception);
   }

   internal async Task<(bool, string)> GetCachedScript(string scriptPath)
   {
      var cacheFolder = await GetHuskyCacheFolder();
      await using var fileStream = _fileSystem.FileStream.New(scriptPath, FileMode.Open);

      var hash = await CalculateHashAsync(fileStream);

      var cachedScriptPath = _fileSystem.Path.Combine(cacheFolder, hash + EXTENSION);

      return (_fileSystem.File.Exists(cachedScriptPath), cachedScriptPath);
   }

   internal static async Task<string> CalculateHashAsync(Stream stream)
   {
      using var sha512 = SHA512.Create();
      var computedHashBytes = await sha512.ComputeHashAsync(stream);
      return Convert.ToHexString(computedHashBytes);
   }

   internal async Task<string> GetHuskyCacheFolder()
   {
      var gitPath = await _git.GetGitPathAsync();
      var huskyFolder = await _git.GetHuskyPathAsync();
      var huskyIgnorePath = _fileSystem.Path.Combine(gitPath, huskyFolder, "_");

      if (!_fileSystem.Directory.Exists(huskyIgnorePath))
         throw new CommandException("can not find husky required files (try: husky install)");

      var cacheFolder = _fileSystem.Path.Combine(huskyIgnorePath, "cache");
      if (!_fileSystem.Directory.Exists(cacheFolder))
         _fileSystem.Directory.CreateDirectory(cacheFolder);

      return cacheFolder;
   }
}
