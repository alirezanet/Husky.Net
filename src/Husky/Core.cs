using System.Diagnostics;

namespace Husky;

public static class Core
{
   private const string HUSKY_FOLDER_NAME = ".husky";

// Git command
   public static Process Git(params string[] args)
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
         p.OutputDataReceived += (_, e) => e.Data.Log(false);
         p.ErrorDataReceived += (_, e) => e.Data?.LogErr();
         p.Start();
         p.BeginOutputReadLine();
         p.BeginErrorReadLine();
         p.WaitForExit();
         return p;
      }
      catch (Exception)
      {
         "Git not found or is not installed".Log();
         Environment.Exit(2);
         throw;
      }
   }

   public static void Install(string? dir = null)
   {
      // Ensure that we're inside a git repository
      // If git command is not found, status is null and we should return.
      // That's why status value needs to be checked explicitly.
      if (Git("rev-parse").ExitCode != 0)
         throw new Exception("Not a git repository");

      // Custom dir help TODO: change this url to short version of docs
      const string url = "https://github.com/alirezanet/husky.net";

      var cwd = Environment.CurrentDirectory;
      // set default husky folder
      dir ??= Path.Combine(cwd, HUSKY_FOLDER_NAME);

      // Ensure that we're not trying to install outside of cwd
      if (!dir.StartsWith(cwd))
         throw new Exception($".. not allowed (see {url})");

      // Ensure that cwd is git top level
      if (!Directory.Exists(".git"))
         throw new Exception($".git can't be found (see {url})");

      try
      {
         // Create .husky/_
         Directory.CreateDirectory(Path.Combine(dir, "_"));

         // Create .husky/_/.  ignore
         File.WriteAllText(Path.Combine(dir, "_/.gitignore"), "*");

         // Copy husky.sh to .husky/_/husky.sh
         File.Copy(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? cwd, "husky.sh"), Path.Combine(dir, "_/husky.sh"));

         // Configure repo
         var p = Git("config", "core.hooksPath", dir);
         if (p.ExitCode != 0)
            throw new Exception("Failed to configure git");
      }
      catch (Exception)
      {
         "Git hooks failed to install".Log();
         throw;
      }

      "Git hooks installed".Log();
   }

   public static void Uninstall()
   {
      var p = Git("config", "--unset", "core.hooksPath");
      if (p.ExitCode == 0)
         "Git hooks successfully uninstalled".Log(false);
      else // todo: remove this ?
         p.ExitCode.ToString().Log(false);
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

      $"created {file}".Log();
   }

   public static void Add(string file, string cmd)
   {
      if (File.Exists(file))
      {
         File.AppendAllText(file, $"{cmd}\n");
         "added to hook".Log();
      }
      else
         Set(file, cmd);
   }
}
