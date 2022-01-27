namespace Husky.TaskRunner;

public class ArgumentInfo
{
   public ArgumentInfo(ArgumentTypes argumentTypes, string argument)
   {
      Argument = argument;
      ArgumentTypes = argumentTypes;
   }

   public string Argument { get; set; }
   public ArgumentTypes ArgumentTypes { get; }
}
