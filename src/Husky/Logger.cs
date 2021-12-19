namespace Husky;

public static class Logger
{
   public static void Log(this string? message, bool isHusky = true)
   {
      if (string.IsNullOrWhiteSpace(message)) return;
      var msg = isHusky ? $"[Husky] - {message}" : message;
      Console.WriteLine(msg);
   }
}
