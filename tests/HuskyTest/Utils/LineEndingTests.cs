using FluentAssertions;
using Husky.Utils;
using Xunit;

namespace HuskyTest.Utils;

public class LineEndingTests
{
   [Fact]
   public void NormalizeShellScriptLineEndings_ShouldConvertCrLfToLf()
   {
      var content = "#!/bin/sh\r\necho husky\r\n";

      var normalized = ShellScriptLineEndings.Normalize(content);

      normalized.Should().Be("#!/bin/sh\necho husky\n");
      normalized.Should().NotContain("\r");
   }

   [Fact]
   public void NormalizeShellScriptLineEndings_ShouldConvertStandaloneCrToLf()
   {
      var content = "#!/bin/sh\recho husky\r";

      var normalized = ShellScriptLineEndings.Normalize(content);

      normalized.Should().Be("#!/bin/sh\necho husky\n");
      normalized.Should().NotContain("\r");
   }
}
