using DotNet.Testcontainers.Containers;

namespace HuskyIntegrationTests;

public static class Extensions
{
   public const string SuccessfullyExecuted = "✔ Successfully executed";
   public const string Skipped = "💤 Skipped, no matched files";

   public static async Task<ExecResult> BashAsync(this IContainer container, params string[] command)
   {
      var result = await container.ExecAsync(["/bin/bash", "-c", ..command]);
      if (result.ExitCode != 0)
         throw new Exception(result.Stderr);
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
}
