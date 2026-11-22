using System.IO.Abstractions;
using System.Xml.Linq;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Services.Contracts;
using Husky.Stdout;

namespace Husky.Cli;

[Command("attach", Description = "Add husky as a dev-dependency to your project")]
public class AttachCommand : CommandBase
{
   private readonly IGit _git;
   private readonly IFileSystem _io;
   private readonly IXmlIO _xmlIo;

   public AttachCommand(IGit git, IFileSystem io, IXmlIO xmlIo)
   {
      _git = git;
      _io = io;
      _xmlIo = xmlIo;
   }

   [CommandParameter(0, Description = "Path to the project (vbproj/csproj/etc) file.")]
   public string FileName { get; set; } = default!;

   [CommandOption("force", 'f', Description = "This will overwrite the existing husky target tag if it exists.")]
   public bool Force { get; set; } = default!;

   [CommandOption("ignore-submodule", Description = "Ignore the MsBuild target when it's a git submodule (IgnoreSubmodule != 0)")]
   public bool IgnoreSubmodule { get; set; }

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var currentDirectory = _io.Directory.GetCurrentDirectory();
      var filepath = Path.IsPathFullyQualified(FileName) ? FileName : Path.Combine(currentDirectory, FileName);
      var doc = _xmlIo.Load(filepath);

      var huskyTarget = doc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value.Equals("Husky", StringComparison.InvariantCultureIgnoreCase) ?? false);

      // If husky target tag exists, we should exit
      if (huskyTarget != null && !Force)
      {
         "Husky is already attached to this project.".Log(ConsoleColor.Yellow);
         return;
      }

      // Remove existing husky elements to avoid duplicates
      foreach (var target in doc.Descendants("Target")
         .Where(q => q.Attribute("Name")?.Value.Equals("Husky", StringComparison.InvariantCultureIgnoreCase) ?? false)
         .ToList())
      {
         target.Remove();
      }

      foreach (var huskyRoot in doc.Descendants("HuskyRoot").ToList())
      {
         var pg = huskyRoot.Parent;
         huskyRoot.Remove();
         if (pg is { HasElements: false })
         {
            pg.Remove();
         }
      }

      // create husky target
      await AddHuskyTarget(doc, filepath);
      _xmlIo.Save(filepath, doc);

      "Husky dev-dependency successfully attached to this project.".Log(ConsoleColor.Green);
   }

   private async Task AddHuskyTarget(XContainer doc, string filepath)
   {
      var condition = GetCondition(doc);
      var rootRelativePath = await GetRelativePath(filepath);

      // Normalize to forward slashes and ensure trailing slash for MSBuild string concatenation
      var huskyRoot = rootRelativePath
         .Replace(Path.DirectorySeparatorChar, '/')
         .TrimEnd('/')
         + "/";

      var propertyGroup = new XElement("PropertyGroup");
      var huskyRootElement = new XElement("HuskyRoot", huskyRoot);
      huskyRootElement.SetAttributeValue("Condition", "'$(HuskyRoot)' == ''");
      propertyGroup.Add(huskyRootElement);
      doc.Add(propertyGroup);

      var target = new XElement("Target");
      target.SetAttributeValue("Name", "Husky");
      target.SetAttributeValue("AfterTargets", "Restore");
      target.SetAttributeValue("Condition", condition + " and !Exists('$(HuskyRoot).husky/_/install.stamp')");
      var exec = new XElement("Exec");
      exec.SetAttributeValue("Command", "dotnet tool restore");
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");
      target.Add(exec);
      exec = new XElement("Exec");
      exec.SetAttributeValue("Command", GetInstallCommand());
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");
      exec.SetAttributeValue("WorkingDirectory", "$(HuskyRoot)");
      target.Add(exec);

      var touch = new XElement("Touch");
      touch.SetAttributeValue("Files", "$(HuskyRoot).husky/_/install.stamp");
      touch.SetAttributeValue("AlwaysCreate", "true");
      touch.SetAttributeValue("Condition", "Exists('$(HuskyRoot).husky/_')");
      target.Add(touch);

      doc.Add(target);

      // The stamp is written from a target that runs AfterTargets="Restore", so it is
      // never recorded in the build's FileListAbsolute.txt and `dotnet clean` cannot
      // remove it. Delete it explicitly after Clean so the next build re-installs
      // (e.g. after a tool version bump).
      var cleanTarget = new XElement("Target");
      cleanTarget.SetAttributeValue("Name", "HuskyClean");
      cleanTarget.SetAttributeValue("AfterTargets", "Clean");
      var delete = new XElement("Delete");
      delete.SetAttributeValue("Files", "$(HuskyRoot).husky/_/install.stamp");
      cleanTarget.Add(delete);
      doc.Add(cleanTarget);
   }

   private string GetInstallCommand()
   {
      var parameters = IgnoreSubmodule ? " --ignore-submodule" : string.Empty;
      return $"dotnet husky install{parameters}";
   }

   private async Task<string> GetRelativePath(string filepath)
   {
      var gitPath = await _git.GetGitPathAsync();
      var fileInfo = _io.FileInfo.New(filepath);
      var relativePath = Path.GetRelativePath(fileInfo.DirectoryName!, gitPath);
      return relativePath;
   }

   private string GetCondition(XContainer doc)
   {
      var condition = "'$(HUSKY)' != 0";
      if (IgnoreSubmodule)
      {
         condition += " and '$(IgnoreSubmodule)' != 0";
      }
      var targetFrameworks = doc.Descendants("PropertyGroup").Descendants("TargetFrameworks").FirstOrDefault();
      if (targetFrameworks != null && targetFrameworks.Value.Contains(';')) condition += " and '$(IsCrossTargetingBuild)' == 'true'";

      return condition;
   }
}
