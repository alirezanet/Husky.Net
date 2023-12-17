using Microsoft.Extensions.Logging;

namespace HuskyIntegrationTests;

public class DockerLogger(ITestOutputHelper output) : ILogger
{
   public IDisposable BeginScope<TState>(TState state)
{
   return null!; // You can implement if needed
}

public bool IsEnabled(LogLevel logLevel)
{
   // Adjust the log level as needed
   return true;
}

public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
{
   if (!IsEnabled(logLevel))
   {
      return;
   }

   var logMessage = formatter(state, exception);

   // Write to ITestOutputHelper
   output.WriteLine($"[{logLevel}] {logMessage}");
}
}
