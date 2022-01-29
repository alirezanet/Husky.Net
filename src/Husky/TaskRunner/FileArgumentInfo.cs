namespace Husky.TaskRunner;

public class FileArgumentInfo : ArgumentInfo
{
   public PathModes PathMode { get; }
   public string RelativePath { get; }
   public string AbsolutePath { get; }

   public FileArgumentInfo(
       ArgumentTypes argumentTypes,
       PathModes pathMode,
       string relativePath,
       string absolutePath = ""
   ) : base(argumentTypes, pathMode == PathModes.Relative ? relativePath : absolutePath)
   {
      PathMode = pathMode;
      RelativePath = relativePath;
      AbsolutePath = absolutePath;
   }

   public string GetPath()
   {
      return PathMode switch
      {
         PathModes.Relative => RelativePath,
         PathModes.Absolute => AbsolutePath,
         _ => throw new ArgumentOutOfRangeException()
      };
   }
}
