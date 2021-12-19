namespace Husky;

public static class Logger
{
   public static void Husky(this string message, ConsoleColor? color = null)
   {
      if (color != null)
         Console.ForegroundColor = color.Value;

      Console.WriteLine($"[Husky] - {message}");
      Console.ResetColor();
   }

   public static void Log(this string message)
   {
      Console.Write(message);
   }

   public static void LogErr(this string message)
   {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"{message}");
      Console.ResetColor();
   }
}
