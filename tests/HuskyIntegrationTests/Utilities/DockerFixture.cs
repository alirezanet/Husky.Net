using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

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
