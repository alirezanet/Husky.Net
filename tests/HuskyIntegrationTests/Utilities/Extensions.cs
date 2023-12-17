using DotNet.Testcontainers.Containers;

namespace HuskyIntegrationTests;

public static class Extensions
{
   public const string SuccessfullyExecuted = "✔ Successfully executed";

   public static Task<ExecResult> BashAsync(this IContainer container, params string[] command)
   {
      return container.ExecAsync(["/bin/bash", "-c", ..command]);
   }

   public static async Task<ExecResult> BashAsync(this IContainer container, ITestOutputHelper output, params string[] command)
   {
      var result = await container.ExecAsync(["/bin/bash", "-c", ..command]);

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
}
