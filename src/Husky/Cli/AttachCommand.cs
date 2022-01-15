using System.Xml.Linq;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Stdout;
using Husky.Utils;

namespace Husky.Cli;

[Command("attach", Description = "Add husky as a dev-dependency to your project")]
public class AttachCommand : CommandBase
{
   [CommandParameter(0, Description = "Path to the project (vbproj/csproj/etc) file.")]
   public string FileName { get; set; } = "";

   [CommandOption("force", 'f', Description = "This will overwrite the existing husky target tag if it exists.")]
   public bool Force { get; set; } = false;

   protected override async ValueTask SafeExecuteAsync(IConsole console)
   {
      var currentDirectory = Directory.GetCurrentDirectory();
      var filepath = Path.IsPathFullyQualified(FileName) ? FileName : Path.Combine(currentDirectory, FileName);
      var doc = XElement.Load(filepath);

      var huskyTarget = doc.Descendants("Target")
         .FirstOrDefault(q => q.Attribute("Name")?.Value.Equals("Husky", StringComparison.InvariantCultureIgnoreCase) ?? false);

      // If husky target tag exists, we should exit
      if (huskyTarget != null && !Force)
      {
         "Husky is already attached to this project.".Log(ConsoleColor.Yellow);
         return;
      }

      // If husky target tag exists, remove it
      if (huskyTarget != null && Force)
         huskyTarget.Remove();

      // create husky target
      var target = new XElement("Target");
      target.SetAttributeValue("Name", "Husky");
      target.SetAttributeValue("BeforeTargets", "Restore;CollectPackageReferences");
      target.SetAttributeValue("Condition", GetCondition(doc));
      var exec = new XElement("Exec");
      exec.SetAttributeValue("Command", "dotnet tool restore");
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");
      target.Add(exec);
      exec = new XElement("Exec");
      exec.SetAttributeValue("Command", "dotnet husky install");
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");

      var relativePath = await GetRelativePath(filepath);
      exec.SetAttributeValue("WorkingDirectory", relativePath);
      target.Add(exec);
      doc.Add(target);
      doc.Save(filepath);

      "Husky dev-dependency successfully attached to this project.".Log(ConsoleColor.Green);
   }

   private static async Task<string> GetRelativePath(string filepath)
   {
      var git = new Git();
      var gitPath = await git.GetGitPathAsync();
      var fileInfo = new FileInfo(filepath);
      var relativePath = Path.GetRelativePath(fileInfo.DirectoryName!, gitPath);
      return relativePath;
   }

   private static string GetCondition(XContainer doc)
   {
      var condition = "'$(HUSKY)' != 0";
      var targetFrameworks = doc.Descendants("PropertyGroup").Descendants("TargetFrameworks").FirstOrDefault();
      if (targetFrameworks != null && targetFrameworks.Value.Contains(";")) condition += " and '$(IsCrossTargetingBuild)' == 'true'";

      return condition;
   }
}
