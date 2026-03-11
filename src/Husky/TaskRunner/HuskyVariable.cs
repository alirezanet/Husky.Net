namespace Husky.TaskRunner;

public class HuskyVariable
{
   public string? Name { get; set; }
   public string? Command { get; set; }
   public string[]? Args { get; set; }
   public string? Cwd { get; set; }

   /// <summary>
   /// When true, files returned by this variable will be treated as staged files,
   /// enabling re-staging after formatting (same behavior as the built-in ${staged} variable).
   /// </summary>
   public bool Staged { get; set; }
}
