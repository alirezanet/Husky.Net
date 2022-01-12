namespace Husky.Stdout;

public static class LoggerEx
{
   public static ILogger logger { get; set; } = default!;

   public static void Husky(this string message, ConsoleColor? color = null)
   {
      logger.Husky(message, color);
   }

   public static void Log(this string message, ConsoleColor? color = null)
   {
      logger.Log(message, color);
   }

   public static void Hr(int count = 50, ConsoleColor? color = ConsoleColor.DarkGray)
   {
      logger.Hr(count, color);
   }

   public static void LogVerbose(this string message, ConsoleColor color = ConsoleColor.DarkGray)
   {
      logger.LogVerbose(message, color);
   }

   public static void LogErr(this string message)
   {
      logger.LogErr(message);
   }
}
