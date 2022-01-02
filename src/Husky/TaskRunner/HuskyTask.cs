namespace Husky.TaskRunner;

public class HuskyTask
{
   public string? Command { get; set; }
   public string[]? Args { get; set; }
   public string? Name { get; set; }
   public OutputTypes? Output { get; set; }
   public PathModes? PathMode { get; set; }
   public string? Cwd { get; set; }
   public string? Group { get; set; }
   public string? Branch { get; set; }
   public HuskyTask? Windows { get; set; }
   public string[]? Include { get; set; }
   public string[]? Exclude { get; set; }
}
