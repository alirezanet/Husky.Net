using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace HuskyIntegrationTests.Utilities;

public static class GlobalImageBuilder
{
   /// <summary>
   /// Set this value to false if you don't want the image to be removed after the test
   /// This is useful if you want to debug the image, or fix tests issues
   /// </summary>
   private const bool RemoveHuskyImageAfterTest = true;


   private static bool _imageBuilt;
   private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

   public static async ValueTask BuildImageAsync()
   {
      if (_imageBuilt)
      {
         return;
      }

      await SemaphoreSlim.WaitAsync();
      try
      {
         if (!_imageBuilt && !await ImageExistsAsync("husky"))
         {
            var image = new ImageFromDockerfileBuilder()
               .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
               .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
               .WithDockerfile("Dockerfile")
               .WithName("husky")
               .WithCleanUp(RemoveHuskyImageAfterTest)
               .Build();

            await image.CreateAsync();
            _imageBuilt = true;
         }
      }
      finally
      {
         SemaphoreSlim.Release();
      }
   }

   private static async Task<bool> ImageExistsAsync(string imageName)
   {
      var clientConfiguration = TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientConfiguration();
      using var dockerClient = clientConfiguration.CreateClient();
      var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters
      {
         Filters = new Dictionary<string, IDictionary<string, bool>>
         {
            ["reference"] = new Dictionary<string, bool> { [imageName] = true }
         }
      });
      return _imageBuilt = images.Count > 0;
   }
}
