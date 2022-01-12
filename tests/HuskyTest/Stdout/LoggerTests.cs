using System.Runtime.InteropServices;
using CliFx.Infrastructure;
using FluentAssertions;
using Husky.Stdout;
using Xunit;

namespace HuskyTest.Stdout;

public class LoggerTests
{
   [Fact]
   public void Husky_WhenHuskyQuietIsTrue_ShouldIgnoreHuskyLogs()
   {
      // Arrange
      var console = new FakeInMemoryConsole();
      var logger = new Logger(console)
      {
         HuskyQuiet = true
      };

      // Act
      logger.Husky("Test message");

      // Assert
      console.ReadOutputString().Should().BeEmpty();
   }

   [Fact]
   public void LogVerbose_WhenVerboseIsFalse_ShouldIgnoreVerboseLogs()
   {
      // Arrange
      var console = new FakeInMemoryConsole();
      var logger = new Logger(console)
      {
         Verbose = false
      };

      // Act
      logger.LogVerbose("Test message");

      // Assert
      console.ReadOutputString().Should().BeEmpty();
   }

   [Fact]
   public void LogVerbose_WhenVerboseIsTrue_ShouldWriteVerboseLogs()
   {
      // Arrange
      var console = new FakeInMemoryConsole();
      var logger = new Logger(console)
      {
         Verbose = true
      };

      // Act
      logger.LogVerbose("Test message");

      // Assert
      console.ReadOutputString().Should().NotBeEmpty();
   }

   [Fact]
   public void LogErr_ShouldWriteToTheErrorStream()
   {
      // Arrange
      var console = new FakeInMemoryConsole();
      var logger = new Logger(console);

      // Act
      logger.LogErr("Test error message");

      // Assert
      console.ReadOutputString().Should().BeEmpty();
      console.ReadErrorString().Should().NotBeEmpty();
   }

   [Fact]
   public void LogErr_OnWindowsWhenVt100IsTrue_ShouldUseVt100Colors()
   {
      // Arrange
      var console = new FakeInMemoryConsole();
      var logger = new Logger(console) { Vt100Colors = true };

      // Act
      logger.LogErr("Test error message");

      // Assert
      console.ReadOutputString().Should().BeEmpty();
      console.ReadErrorString().Should().NotBeEmpty();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
         console.ReadErrorString().Should().Contain("\x1b[0m");
      }
   }

}
