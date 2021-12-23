namespace Husky;

public partial class HuskyTask
{
   public string? Command { get; set; }
   public string[]? Args { get; set; }
   public string? Name { get; set; }
   public OutputTypes Output { get; set; } = OutputTypes.Error;
   public PathModes PathMode { get; set; } = PathModes.Relative;
   public string? Cwd { get; set; }
   public string? Group { get; set; }

   public string[]? Include
   {
      get; set;
   }
   public string[]? Exclude { get; set; }


}
