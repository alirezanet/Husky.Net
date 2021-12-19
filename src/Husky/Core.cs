using System.Diagnostics;
using System.Reflection;

namespace Husky;

public static class Core
{
   private const string HUSKY_FOLDER_NAME = ".husky";

   // Custom dir help TODO: change this url to short version of docs
   public const string DOCS_URL = "https://github.com/alirezanet/husky.net";

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

   public static void Install(string? dir = null)
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
   }

   public static void Uninstall()
   {
      var p = Git("config", "--unset", "core.hooksPath");
      if (p.ExitCode == 0)
         "Git hooks successfully uninstalled".Husky(ConsoleColor.Green);
   }

   public static void Set(string file, string cmd)
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
   }

   public static void Add(string file, string cmd)
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
   }
}
