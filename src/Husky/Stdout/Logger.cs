namespace Husky.Stdout;

public static class Logger
{
   public static bool Verbose = false;
   public static bool Colors = true;
   public static bool Vt100Colors = false;
   public static bool Quiet = false;

   private static void Write(string message, ConsoleColor? color = null)
   {
      if (Colors && color != null)
      {
         if (Vt100Colors)
         {
            Vt100.Write(message, color.Value);
         }
         else
         {
            Console.ForegroundColor = color.Value;
            Console.Write(message);
            Console.ResetColor();
         }
      }
      else
         Console.Write(message);
   }

   private static void WriteLine(string message, ConsoleColor? color = null)
   {
      Write(message, color);
      Write(Environment.NewLine);
   }

   public static void Husky(this string message, ConsoleColor? color = null)
   {
      if (Quiet) return;
      Write("[Husky] ", ConsoleColor.Cyan);
      WriteLine($"{message}", color);
   }

   public static void Log(this string message, ConsoleColor? color = null)
   {
      WriteLine(message, color);
   }

   public static void Hr(int count = 50, ConsoleColor? color = ConsoleColor.DarkGray)
   {
      if (Quiet) return;
      WriteLine(new string('-', count), color);
   }

   public static void LogVerbose(this string message, ConsoleColor color = ConsoleColor.DarkGray)
   {
      if (!Verbose) return;
      WriteLine(message, color);
   }

   public static void LogErr(this string message)
   {
      Log(message, ConsoleColor.Red);
   }
}
