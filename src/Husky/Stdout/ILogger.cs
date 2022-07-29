namespace Husky.Stdout;

public interface ILogger
{
   public bool Colors { get; set; }
   public bool Verbose { get; set; }
   public bool Vt100Colors { get; set; }
   public bool HuskyQuiet { get; set; }
   public bool NoUnicode { get; set; }

   void Husky(string message, ConsoleColor? color = null);
   void Log(string message, ConsoleColor? color = null);
   void Hr(int count = 50, ConsoleColor? color = ConsoleColor.DarkGray);
   void LogVerbose(string message, ConsoleColor color = ConsoleColor.DarkGray);
   void LogErr(string message);
}
