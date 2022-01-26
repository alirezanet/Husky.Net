namespace Husky.TaskRunner;

public class ArgumentInfo
{
   public ArgumentInfo(string argument, ArgumentTypes argumentTypes)
   {
      Argument = argument;
      ArgumentTypes = argumentTypes;
   }
   public string Argument { get; set; }
   public ArgumentTypes ArgumentTypes { get; }
}

public enum ArgumentTypes
{
   Static,
   CustomArgument,
   File,
   StagedFile,
   CustomVariable
}
