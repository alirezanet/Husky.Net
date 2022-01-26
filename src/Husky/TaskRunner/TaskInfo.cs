namespace Husky.TaskRunner;

public class TaskInfo
{
   public TaskInfo(
       string name,
       string command,
       string[] arguments,
       string workingDirectory,
       OutputTypes outputType,
       ArgumentInfo[] argumentInfo
   )
   {
      Name = name;
      Command = command;
      Arguments = arguments;
      WorkingDirectory = workingDirectory;
      OutputType = outputType;
      ArgumentInfo = argumentInfo;
   }

   public string Name { get; set; }
   public string Command { get; set; }
   public string[] Arguments { get; set; }
   public string WorkingDirectory { get; set; }
   public OutputTypes OutputType { get; set; }
   public ArgumentInfo[] ArgumentInfo { get; set; }
}
