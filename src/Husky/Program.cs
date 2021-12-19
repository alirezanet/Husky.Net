using System.Diagnostics;
using Husky;

const string HUSKY = ".husky";
const string DIRNAME = ".husky";

// Git command
Process Git(params string[] args)
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
      p.ErrorDataReceived += (_, e) => e.Data.Log(false);
      p.Start();
      p.BeginOutputReadLine();
      p.BeginErrorReadLine();
      p.WaitForExit();
      return p;
   }
   catch (Exception e)
   {
      "Git not found or is not installed".Log();
      Environment.Exit(2);
      throw;
   }
}

void Install(string dir = HUSKY)
{
   // Ensure that we're inside a git repository
   // If git command is not found, status is null and we should return.
   // That's why status value needs to be checked explicitly.
   if (Git("rev-parse").ExitCode != 0)
      return;

   // Custom dir help TODO: change this url to husky.NET repo
   const string url = "https://git.io/Jc3F9";

   // Ensure that we're not trying to install outside of cwd
   var cwd = Directory.GetCurrentDirectory();
   if (!Path.Combine(cwd, dir).StartsWith(cwd))
      throw new Exception($".. not allowed (see ${url})");

   // Ensure that cwd is git top level
   if (!Directory.Exists(".git"))
      throw new Exception($".git can't be found (see ${url})");

   try
   {
      // Create .husky/_
      Directory.CreateDirectory(Path.Combine(dir, "_"));

      // Create .husky/_/.  ignore
      File.WriteAllText(Path.Combine(dir, "_/.gitignore"), "*");

      // Copy husky.sh to .husky/_/husky.sh
      File.Copy(Path.Combine(dir,DIRNAME,"../husky.sh"), Path.Combine(dir, "_/husky.sh"));

      // Configure repo
      var p = Git("config", "core.hooksPath", dir) ;
      if (p.ExitCode != 0)
         throw new Exception("Failed to configure git");
   }
   catch (Exception e)
   {
      "Git hooks failed to install".Log();
      throw;
   }
   "Git hooks installed".Log();
}

void Uninstall(string dir = HUSKY)
{
   Git("config", "--unset", "core.hooksPath");
}

void Set(string file, string cmd)
{
   var dir = Path.GetDirectoryName(file);
   if (!Directory.Exists(dir))
      throw new Exception("can't create hook, ${dir} directory doesn't exist (try running husky install)");

   var content = @"#!/bin/sh
. ""$(dirname ""$0"")/_/husky.sh""

      ${cmd}
";
   File.WriteAllText(file,content);

   $"created {file}".Log();
}

void Add(string file, string cmd)
{
   if (File.Exists(file))
      File.AppendAllText(file, $"{cmd}\n");
   else
      Set(file,cmd);
}
