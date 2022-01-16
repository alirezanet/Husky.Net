using System.IO.Abstractions;
using System.Xml.Linq;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Husky.Cli.AttachServices;
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

      // If husky target tag exists, remove it
      if (huskyTarget != null && Force)
         huskyTarget.Remove();

      // create husky target
      var condition = GetCondition(doc);
      var rootRelativePath = await GetRelativePath(filepath);
      var target = GetTarget(condition, rootRelativePath);
      doc.Add(target);
      _xmlIo.Save(filepath, doc);

      "Husky dev-dependency successfully attached to this project.".Log(ConsoleColor.Green);
   }

   public static XElement GetTarget(string condition, string rootRelativePath)
   {
      var target = new XElement("Target");
      target.SetAttributeValue("Name", "Husky");
      target.SetAttributeValue("BeforeTargets", "Restore;CollectPackageReferences");
      target.SetAttributeValue("Condition", condition);
      var exec = new XElement("Exec");
      exec.SetAttributeValue("Command", "dotnet tool restore");
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");
      target.Add(exec);
      exec = new XElement("Exec");
      exec.SetAttributeValue("Command", "dotnet husky install");
      exec.SetAttributeValue("StandardOutputImportance", "Low");
      exec.SetAttributeValue("StandardErrorImportance", "High");
      exec.SetAttributeValue("WorkingDirectory", rootRelativePath);
      target.Add(exec);
      return target;
   }

   private async Task<string> GetRelativePath(string filepath)
   {
      var gitPath = await _git.GetGitPathAsync();
      var fileInfo = _io.FileInfo.FromFileName(filepath);
      var relativePath = Path.GetRelativePath(fileInfo.DirectoryName!, gitPath);
      return relativePath;
   }

   private static string GetCondition(XContainer doc)
   {
      var condition = "'$(HUSKY)' != 0";
      var targetFrameworks = doc.Descendants("PropertyGroup").Descendants("TargetFrameworks").FirstOrDefault();
      if (targetFrameworks != null && targetFrameworks.Value.Contains(';')) condition += " and '$(IsCrossTargetingBuild)' == 'true'";

      return condition;
   }
}
