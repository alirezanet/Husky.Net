namespace Husky.Utils;

public static class ShellScriptLineEndings
{
   public static string Normalize(string content)
   {
      return content.Replace("\r\n", "\n").Replace("\r", "\n");
   }
}
