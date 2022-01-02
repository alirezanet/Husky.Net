using System.Runtime.InteropServices;

namespace Husky.Logger;

internal static class Win32Console
{
   [DllImport("kernel32.dll")]
   private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

   [DllImport("kernel32.dll")]
   private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

   [DllImport("kernel32.dll", SetLastError = true)]
   private static extern IntPtr GetStdHandle(int nStdHandle);

   [DllImport("kernel32.dll")]
   public static extern uint GetLastError();

#pragma warning disable IDE1006 // Naming Styles
   private const int STD_OUTPUT_HANDLE = -11;
   private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

   // private const int STD_INPUT_HANDLE = -10;
   // private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
   // private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
   // private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
   // private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;

#pragma warning restore IDE1006 // Naming Styles

   public static void Initialize()
   {
      var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

      if (GetConsoleMode(iStdOut, out var outConsoleMode))
      {
         outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
         SetConsoleMode(iStdOut, outConsoleMode);
      }
   }
}
