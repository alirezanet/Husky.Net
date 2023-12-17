using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using HuskyIntegrationTests;

public class DockerFixture : IAsyncDisposable
{
   public IFutureDockerImage? Image { get; set; }

   private void BuildImage()
   {
      Image = new ImageFromDockerfileBuilder()
         .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
         .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
         .WithDockerfile("Dockerfile")
         .WithName("husky")
         .WithCleanUp(false)
         .Build();

      Image.CreateAsync().GetAwaiter().GetResult();
   }

   public async Task<IContainer> CopyAndStartAsync(string folderNameToCopy, [CallerMemberName] string name = null!)
   {
      var container = new ContainerBuilder()
         .WithResourceMapping(GetTestFolderPath(folderNameToCopy), "/test/")
         .WithName(GenerateContainerName(name))
         .WithImage("husky")
         .WithWorkingDirectory("/test/")
         .WithEntrypoint("/bin/bash", "-c")
         .WithCommand("tail -f /dev/null")
         .WithImagePullPolicy(response =>
         {
            if (response == null)
            {
               BuildImage();
            }

            return false;
         })
         .Build();

      await container.StartAsync();
      return container;
   }

   public async Task<IContainer> StartAsync([CallerMemberName] string name = null!)
   {
      var container = new ContainerBuilder()
         .WithName(GenerateContainerName(name))
         .WithImage("husky")
         .WithWorkingDirectory("/test/")
         .WithEntrypoint("/bin/bash", "-c")
         .WithCommand("tail -f /dev/null")
         .WithImagePullPolicy(response =>
         {
            if (response == null)
            {
               BuildImage();
            }

            return false;
         })
         .Build();

      await container.StartAsync();
      return container;
   }


   public async Task<IContainer> StartWithInstalledHusky([CallerMemberName] string name = null!)
   {
      var c = await CopyAndStartAsync(nameof(TestProjectBase), name);
      await c.BashAsync("git init");
      await c.BashAsync("dotnet tool restore");
      await c.BashAsync("dotnet husky install");
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

   public ValueTask DisposeAsync()
   {
      return Image?.DisposeAsync() ?? ValueTask.CompletedTask;
   }
}
