using DotNet.Testcontainers.Containers;

namespace HuskyIntegrationTests;

public static class Extensions
{
   public static Task<ExecResult> BashAsync(this IContainer container, params string[] command)
   {
      return container.ExecAsync(["/bin/bash", "-c", ..command]);
   }

   public static async Task<ExecResult> BashAsync(this IContainer container, ITestOutputHelper output, params string[] command)
   {
      var result = await container.ExecAsync(["/bin/bash", "-c", ..command]);
      if (!string.IsNullOrEmpty(result.Stderr))
         output.WriteLine(result.Stderr);

      if (!string.IsNullOrEmpty(result.Stdout))
         output.WriteLine(result.Stdout);

      return result;
   }
}
