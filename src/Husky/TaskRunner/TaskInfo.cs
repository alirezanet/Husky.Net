namespace Husky.TaskRunner;

public class TaskInfo
{
   public TaskInfo(string name, string command, string[] arguments, string workingDirectory, OutputTypes outputType)
   {
      Name = name;
      Command = command;
      Arguments = arguments;
      WorkingDirectory = workingDirectory;
      OutputType = outputType;
   }

   public string Name { get; set; }
   public string Command { get; set; }
   public string[] Arguments { get; set; }
   public string WorkingDirectory { get; set; }
   public OutputTypes OutputType { get; set; }
}
