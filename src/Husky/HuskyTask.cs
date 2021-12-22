namespace Husky;

public class HuskyTask
{
   public string? Command { get; set; }
   public string[]? Args { get; set; }
   public string? Name { get; set; }
   public Outputs Output { get; set; }
   public string? Cwd { get; set; }
   public string? Group { get; set; }

   public string[]? Include
   {
      get; set;
   }
   public string[]? Exclude { get; set; }

   public enum Outputs
   {
      Verbose,
      Never,
      Always,
   }
}
