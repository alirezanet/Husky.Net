using CliFx.Infrastructure;

namespace Husky.Stdout;

internal static class Vt100
{
   public static void Write(string message, ConsoleColor color, ConsoleWriter console)
   {
      SetForegroundColor(color, console);
      console.Write(message);
      ResetColors(console);
   }

   public static void WriteLine(string message, ConsoleColor color, ConsoleWriter console)
   {
      SetForegroundColor(color, console);
      console.WriteLine(message);
      ResetColors(console);
   }

   /// <summary>
   /// https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#text-formatting
   /// Git-bash only supports 8 colors provided by the Windows Console.
   /// </summary>
   /// <param name="color"></param>
   /// <param name="console"></param>
   /// <exception cref="ArgumentOutOfRangeException"></exception>
   public static void SetForegroundColor(ConsoleColor color, ConsoleWriter console)
   {
      var colorCode = color switch
      {
         ConsoleColor.Black => 30,
         ConsoleColor.Red or ConsoleColor.DarkRed => 31,
         ConsoleColor.Green or ConsoleColor.DarkGreen => 32,
         ConsoleColor.Yellow or ConsoleColor.DarkYellow => 33,
         ConsoleColor.Blue or ConsoleColor.DarkBlue => 34,
         ConsoleColor.Magenta or ConsoleColor.DarkMagenta => 35,
         ConsoleColor.Cyan or ConsoleColor.DarkCyan => 36,
         ConsoleColor.White or ConsoleColor.DarkGray or ConsoleColor.Gray => 37,
         _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
      };
      console.Write($"\x1b[{colorCode}m");
   }


   public static void ResetColors(ConsoleWriter console)
   {
      console.Write("\x1b[0m");
   }
}
