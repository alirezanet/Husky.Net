using System;

internal static class Vt100
{
   public static void SwitchToAlternateBuffer()
   {
      Console.Write("\x1b[?1049h");
   }

   public static void SwitchToMainBuffer()
   {
      Console.Write("\x1b[?1049l");
   }

   public static void ShowCursor()
   {
      Console.Write("\x1b[?25h");
   }

   public static void HideCursor()
   {
      Console.Write("\x1b[?25l");
   }

   public static void SetCursorPosition(int x, int y)
   {
      Console.Write($"\x1b[{y + 1};{x + 1}H");
   }

   public static void NegativeColors()
   {
      Console.Write("\x1b[7m");
   }

   public static void PositiveColors()
   {
      Console.Write("\x1b[27m");
   }

   public static void Write(string message, ConsoleColor color)
   {
      SetForegroundColor(color);
      Console.Write(message);
      ResetColors();
   }

   public static void WriteLine(string message, ConsoleColor color)
   {
      SetForegroundColor(color);
      Console.WriteLine(message);
      ResetColors();
   }

   /// <summary>
   /// https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences#text-formatting
   /// Git-bash only supports 8 colors provided by the Windows Console.
   /// </summary>
   /// <param name="color"></param>
   /// <exception cref="ArgumentOutOfRangeException"></exception>
   public static void SetForegroundColor(ConsoleColor color)
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
      Console.Write($"\x1b[{colorCode}m");
   }

   public static void SetExtendedForegroundColor(int r, int g, int b)
   {
      Console.Write($"\x1b[38;2;{r};{g};{b}m");
   }

   public static void SetExtendedBackgroundColor(int r, int g, int b)
   {
      Console.Write($"\x1b[48;2;{r};{g};{b}m");
   }

   public static void SetExtendedForegroundColor(ConsoleColor color)
   {
      var (r, g, b) = GetExtendedColor(color);
      SetExtendedForegroundColor(r, g, b);
   }

   public static void SetExtendedBackgroundColor(ConsoleColor color)
   {
      var (r, g, b) = GetExtendedColor(color);
      SetExtendedBackgroundColor(r, g, b);
   }

   private static (int R, int G, int B) GetExtendedColor(ConsoleColor color)
   {
      return color switch
      {
         ConsoleColor.Black => (12, 12, 12),
         ConsoleColor.DarkBlue => (0, 55, 218),
         ConsoleColor.DarkGreen => (19, 161, 14),
         ConsoleColor.DarkCyan => (58, 150, 221),
         ConsoleColor.DarkRed => (197, 15, 31),
         ConsoleColor.DarkMagenta => (136, 23, 152),
         ConsoleColor.DarkYellow => (193, 156, 0),
         ConsoleColor.Gray => (204, 204, 204),
         ConsoleColor.DarkGray => (118, 118, 118),
         ConsoleColor.Blue => (59, 120, 255),
         ConsoleColor.Green => (22, 198, 12),
         ConsoleColor.Cyan => (97, 214, 214),
         ConsoleColor.Red => (231, 72, 86),
         ConsoleColor.Magenta => (180, 0, 158),
         ConsoleColor.Yellow => (249, 241, 165),
         ConsoleColor.White => (242, 242, 242),
         _ => throw new Exception($"Unexpected color: {color}"),
      };
   }

   public static void ResetScrollMargins()
   {
      Console.Write($"\x1b[r");
   }

   public static void SetScrollMargins(int top, int bottom)
   {
      Console.Write($"\x1b[{top};{bottom}r");
   }

   public static void ScrollUp(int lines)
   {
      Console.Write($"\x1b[{lines}S");
   }

   public static void ScrollDown(int lines)
   {
      Console.Write($"\x1b[{lines}T");
   }

   public static void EraseRestOfCurrentLine()
   {
      Console.Write($"\x1b[K");
   }

   public static void ResetColors()
   {
      Console.Write("\x1b[0m");
   }
}
