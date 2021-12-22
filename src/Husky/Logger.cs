namespace Husky;

public static class Logger
{
   public static bool Verbose = false;
   public static bool Colors = true;

   public static void Husky(this string message, ConsoleColor? color = null)
   {
      if (Colors)
         Console.ForegroundColor = ConsoleColor.Cyan;
      Console.Write("[Husky] - ");
      Console.ResetColor();

      if (Colors && color != null)
         Console.ForegroundColor = color.Value;
      Console.Write($"{message}\n");
      Console.ResetColor();
   }

   public static void Log(this string message, ConsoleColor? color = null)
   {
      if (Colors && color != null)
         Console.ForegroundColor = color.Value;

      Console.WriteLine(message);
      Console.ResetColor();
   }

   public static void logVerbose(this string message, ConsoleColor color = ConsoleColor.DarkGray)
   {
      if (!Verbose) return;
      if (Colors)
         Console.ForegroundColor = color;
      Console.WriteLine(message);
      Console.ResetColor();
   }

   public static void LogErr(this string message)
   {
      Log(message, ConsoleColor.Red);
   }
}
