using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace HuskyIntegrationTests.Utilities;

public static class GlobalImageBuilder
{
   /// <summary>
   /// Set this value to false to keep the image between test runs for faster builds via cache busting
   /// Set to true if you want the image removed after tests complete for cleanup
   /// </summary>
   private const bool RemoveHuskyImageAfterTest = false;


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
         if (_imageBuilt)
         {
            return;
         }

         // Use timestamp to bust Docker build cache for Husky package layer
         // This ensures Husky is always rebuilt while keeping base layers cached
         var cachebuuster = DateTime.UtcNow.Ticks.ToString();

         var image = new ImageFromDockerfileBuilder()
            .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
            .WithBuildArgument("CACHE_BUSTER", cachebuuster)
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("Dockerfile")
            .WithName("husky")
            .WithCleanUp(RemoveHuskyImageAfterTest)
            .Build();

         await image.CreateAsync();
         _imageBuilt = true;
      }
      finally
      {
         SemaphoreSlim.Release();
      }
   }
}
