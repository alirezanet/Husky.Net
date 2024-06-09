using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HuskyIntegrationTests.Utilities;

namespace HuskyIntegrationTests;

public static class DockerHelper
{
   public const string SuccessfullyExecuted = "✔ Successfully executed";
   public const string Skipped = "💤 Skipped, no matched files";

   public static async Task<ExecResult> BashAsync(this IContainer container, params string[] command)
   {
      var result = await container.ExecAsync(["/bin/bash", "-c", ..command]);
      if (result.ExitCode != 0)
         throw new Exception(result.Stderr + result.Stdout);
      return result;
   }

   public static async Task<ExecResult> BashAsync(this IContainer container, ITestOutputHelper output, params string[] command)
   {
      var result = await container.ExecAsync(["/bin/bash", "-c", ..command]);
      output.WriteLine($"{string.Join(" ", command)}:");

      if (!string.IsNullOrEmpty(result.Stdout))
         output.WriteLine(result.Stdout);

      if (!string.IsNullOrEmpty(result.Stderr))
         output.WriteLine(result.Stderr);

      return result;
   }

   public static Task<ExecResult> UpdateTaskRunner(this IContainer container, string content)
   {
      return container.BashAsync($"echo -e '{content}' > /test/.husky/task-runner.json");
   }

   public static Task<ExecResult> AddCsharpClass(this IContainer container, string content, string fileName = "Class2.cs")
   {
      return container.BashAsync($"echo -e '{content}' > /test/{fileName}");
   }

   public static async Task<IContainer> StartContainerAsync(string? folderNameToCopy = null, [CallerMemberName] string name = null!)
   {
      await GlobalImageBuilder.BuildImageAsync();
      var builder = new ContainerBuilder()
         .WithName(GenerateContainerName(name))
         .WithImage("husky")
         .WithWorkingDirectory("/test/")
         .WithEntrypoint("/bin/bash", "-c")
         .WithCleanUp(true)
         .WithCommand("tail -f /dev/null");

      if (!string.IsNullOrEmpty(folderNameToCopy))
      {
         builder = builder.WithResourceMapping(GetTestFolderPath(folderNameToCopy), "/test/");
      }

      var container = builder.Build();
      await container.StartAsync();
      return container;
   }

   public static async Task<IContainer> StartWithInstalledHusky([CallerMemberName] string name = null!)
   {
      await GlobalImageBuilder.BuildImageAsync();
      var c = await StartContainerAsync(nameof(TestProjectBase), name);
      await c.BashAsync("git init");
      await c.BashAsync("dotnet new tool-manifest");
      await c.BashAsync("dotnet tool install --no-cache --add-source /app/nupkg/ husky");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("dotnet husky install");
      await c.BashAsync("git config --global user.email \"you@example.com\"");
      await c.BashAsync("git config --global user.name \"Your Name\"");
      await c.BashAsync("git add .");
      await c.BashAsync("git commit -m 'initial commit'");
      return c;
   }

   private static string GenerateContainerName(string name)
   {
      return $"{name}-{Guid.NewGuid().ToString("N")[..4]}";
   }

   private static string GetTestFolderPath(string folderName)
   {
      var baseDirectory = CommonDirectoryPath.GetProjectDirectory().DirectoryPath;
      return Path.Combine(baseDirectory, folderName);
   }
}
