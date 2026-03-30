namespace Husky.TaskRunner;

public class ArgumentInfo
{
   public ArgumentInfo(ArgumentTypes argumentTypes, string argument, bool excludeFromCommandArgs = false)
   {
      Argument = argument;
      ArgumentTypes = argumentTypes;
      ExcludeFromCommandArgs = excludeFromCommandArgs;
   }

   public string Argument { get; set; }
   public ArgumentTypes ArgumentTypes { get; }

   /// <summary>
   /// When true, this argument is metadata-only and should not be included
   /// in the command-line arguments passed to the process. Used by staged file
   /// tracking entries from the ${staged:separator} pattern.
   /// </summary>
   public bool ExcludeFromCommandArgs { get; }
}
