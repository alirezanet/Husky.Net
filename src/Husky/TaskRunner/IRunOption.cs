namespace Husky.TaskRunner;

public interface IRunOption
{
   public string? Name { get; set; }
   public string? Group { get; set; }
   public bool NoPartial { get; set; }
   public IReadOnlyList<string>? Arguments { get; set; }
}
