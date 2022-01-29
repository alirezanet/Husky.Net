using System.Runtime.InteropServices;
using CliFx.Infrastructure;

namespace Husky.Stdout;

public class Logger : ILogger
{
   private IConsole _console;

   public Logger(IConsole console)
   {
      _console = console;
      Init();
   }

   public bool Colors { get; set; } = true;
   public bool Verbose { get; set; }
   public bool Vt100Colors { get; set; }
   public bool HuskyQuiet { get; set; }

   public void Husky(string message, ConsoleColor? color = null)
   {
      if (HuskyQuiet)
         return;
      Write("[Husky] ", ConsoleColor.Cyan);
      WriteLine($"{message}", color);
   }

   public void Log(string message, ConsoleColor? color = null)
   {
      WriteLine(message, color);
   }

   public void Hr(int count = 50, ConsoleColor? color = ConsoleColor.DarkGray)
   {
      if (HuskyQuiet)
         return;
      WriteLine(new string('-', count), color);
   }

   public void LogVerbose(string message, ConsoleColor color = ConsoleColor.DarkGray)
   {
      if (!Verbose)
         return;
      WriteLine(message, color);
   }

   public void LogErr(string message)
   {
      if (Colors && Vt100Colors)
         Vt100.WriteLine(message, ConsoleColor.Red, _console.Error);
      else
         _console.Error.WriteLine(message);
   }

   private void Init()
   {
      if (
          !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
          || Environment.GetEnvironmentVariable("vt100") is not "1"
      )
         return;
      try
      {
         // enabling vt100 colors for windows
         Win32Console.Initialize();
         Vt100Colors = true;
      }
      catch (Exception e)
      {
         LogVerbose(e.Message, ConsoleColor.DarkRed);
      }
   }

   private void Write(string message, ConsoleColor? color = null)
   {
      try
      {
         if (Colors && color != null)
         {
            if (Vt100Colors)
            {
               Vt100.Write(message, color.Value, _console.Output);
            }
            else
            {
               _console.ForegroundColor = color.Value;
               _console.Output.Write(message);
               _console.ResetColor();
            }
         }
         else
         {
            _console.Output.Write(message);
         }
      }
      catch (Exception)
      {
         // TODO: fix clifx bug https://github.com/Tyrrrz/CliFx/issues/123
         // temporary fix the clifx overflow exception
         _console = new SystemConsole();
         _console.Output.Write(message);
      }
   }

   private void WriteLine(string message, ConsoleColor? color = null)
   {
      Write(message, color);
      Write(Environment.NewLine);
   }
}
