using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Husky;

public static class Core
{
   private const string HUSKY_FOLDER_NAME = ".husky";

   // Custom dir help TODO: change this url to short version of docs
   public const string DOCS_URL = "https://github.com/alirezanet/husky.net";


   private static bool GitExec(out string output, params string[] args)
   {
      try
      {
         var pi = new ProcessStartInfo("git")
         {
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true
         };
         var logQueue = new ConcurrentQueue<string>();
         var p = new Process();
         p.StartInfo = pi;
         p.Start();
         var stdout = p.StandardOutput.ReadToEnd().Trim();
         var stderr = p.StandardError.ReadToEnd().Trim();
         p.WaitForExit();
         if (p.ExitCode != 0)
         {
            output = stderr;
            return false;
         }

         output = stdout;
         return true;
      }
      catch (Exception e)
      {
         output = e.Message;
         return false;
      }
   }

   // Git command
   private static Process Git(params string[] args)
   {
      try
      {
         var pi = new ProcessStartInfo("git")
         {
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true
         };
         var p = new Process();
         p.StartInfo = pi;
         p.OutputDataReceived += (_, e) => e.Data?.Log();
         p.ErrorDataReceived += (_, e) => e.Data?.LogErr();
         p.Start();
         p.BeginOutputReadLine();
         p.BeginErrorReadLine();
         p.WaitForExit();
         return p;
      }
      catch (Exception)
      {
         "Git not found or is not installed".Husky();
         Environment.Exit(2);
         throw;
      }
   }


   public static int Install(string? dir = null)
   {
      // Ensure that we're inside a git repository
      // If git command is not found, we should return exception.
      // That's why ExitCode needs to be checked explicitly.
      if (Git("rev-parse").ExitCode != 0)
         throw new Exception("Not a git repository");

      var cwd = Environment.CurrentDirectory;

      // set default husky folder
      dir ??= HUSKY_FOLDER_NAME;
      var path = Path.GetFullPath(Path.Combine(cwd, dir));

      // Ensure that we're not trying to install outside of cwd
      if (!path.StartsWith(cwd))
         throw new Exception($"{cwd}\nnot allowed (see {DOCS_URL})");

      // Ensure that cwd is git top level
      if (!Directory.Exists(Path.Combine(cwd, ".git")))
         throw new Exception($".git can't be found (see {DOCS_URL})");

      try
      {
         // Create .husky/_
         Directory.CreateDirectory(Path.Combine(path, "_"));

         // Create .husky/_/.  ignore
         File.WriteAllText(Path.Combine(path, "_/.gitignore"), "*");

         // Copy husky.sh to .husky/_/husky.sh
         {
            using var stream = Assembly.GetAssembly(typeof(Core))!.GetManifestResourceStream("Husky.husky.sh")!;
            using var sr = new StreamReader(stream);
            var content = sr.ReadToEnd();
            File.WriteAllText(Path.Combine(path, "_/husky.sh"), content);
         }

         // Created staged.json file
         // We don't want to override this file
         if (!File.Exists(Path.Combine(path, "settings.json")))
         {
            const string stagedDefault = @"{
   ""staged"": {
      ""*"": ""echo 'use your cmd instead of `echo ...`'""
   }
}";
            File.WriteAllText(Path.Combine(path, "staged.json"), stagedDefault);
         }

         // Configure repo
         var p = Git("config", "core.hooksPath", dir);
         if (p.ExitCode != 0)
            throw new Exception("Failed to configure git");
      }
      catch (Exception)
      {
         "Git hooks failed to install".Husky();
         throw;
      }

      "Git hooks installed".Husky(ConsoleColor.Green);
      return 0;
   }

   public static int Version()
   {
      var v = Assembly.GetAssembly(typeof(Core))?.GetName().Version?.ToString() ?? throw new Exception("Something is not right!");
      v.Log();
      return 0;
   }

   public static int Uninstall()
   {
      var p = Git("config", "--unset", "core.hooksPath");
      if (p.ExitCode == 0)
         "Git hooks successfully uninstalled".Husky(ConsoleColor.Green);
      return 0;
   }

   public static int Set(string file, string cmd)
   {
      var dir = Path.GetDirectoryName(file);
      if (!Directory.Exists(dir))
         throw new Exception($"can't create hook, {dir} directory doesn't exist (try running husky install)");

      var content = @$"#!/bin/sh
. ""$(dirname ""$0"")/_/husky.sh""

{cmd}
";
      File.WriteAllText(file, content);

      $"created {file}".Husky();

      return 0;
   }

   public static int Add(string file, string cmd)
   {
      if (File.Exists(file))
      {
         File.AppendAllText(file, $"{cmd}\n");
         "added to hook".Husky();
      }
      else
      {
         Set(file, cmd);
      }

      return 0;
   }

   public static int Staged()
   {
      // find git top level path
      if (GitExec(out var gitPath, "rev-parse", "--show-toplevel"))
      {
         // find .husky path from git
         if (GitExec(out var huskyPath, "config", "--get", "core.hooksPath"))
         {
            var dir = Path.Combine(gitPath, huskyPath, "settings.json");
            var config = new ConfigurationBuilder()
               .AddJsonFile(dir)
               .Build();

            var rules = config.GetSection("staged").GetChildren().ToList();

            // find current staged files
            if (GitExec(out var stagedFiles, "diff", "--name-only", "--staged"))
            {
               // we don't need to do anything
               if (stagedFiles.Length == 0) return 0;

               var files = stagedFiles.Split('\n');
               foreach (var rule in rules)
               {
                  var matcher = new Matcher().AddInclude(rule.Key);
                  var matchResult = matcher.Match(gitPath, files);
                  if (!matchResult.HasMatches) continue;

                  // execute command for match files
                  var exitCode = Shell($"{rule.Value} {string.Join(" ", matchResult.Files.Select(q=>q.Path))}");
                  if (exitCode != 0)
                  {
                     $"failed to execute command {rule.Value}".LogErr();
                     return 13;
                  }
               }


            }
         }
      }
      return 0;
   }

   public static int Shell(string shell)
   {
      var command = shell.Split(" ");
      var pi = new ProcessStartInfo
      {
         FileName = command.First(),
         WindowStyle = ProcessWindowStyle.Hidden,
         UseShellExecute = true,
      };
      if (command.Length > 1)
         pi.Arguments = string.Join(" ", command[1..]);

      var p = Process.Start(pi);
      p.StandardOutput.ReadToEnd().Log();
      p?.WaitForExit();
      return p?.ExitCode ?? -1;
   }
}
