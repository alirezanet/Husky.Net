namespace Husky.TaskRunner;

/// <summary>
/// Special class to pass variables to the csx files.
/// </summary>
public class Globals
{
#pragma warning disable CS8618
   // ReSharper disable once UnusedAutoPropertyAccessor.Global
   public IList<string> Args { get; set; }
#pragma warning restore CS8618
}
