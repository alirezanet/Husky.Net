using CliWrap;
using CliWrap.Buffered;
using Husky.TaskRunner;

namespace Husky.Services.Contracts;

public interface ICliWrap
{
   Task<BufferedCommandResult> ExecBufferedAsync(string fileName, string args);
   ValueTask SetExecutablePermission(params string[] files);
   Task<CommandResult> ExecDirectAsync(string fileName, string args);

   Task<CommandResult> RunCommandAsync(string fileName, IEnumerable<string> args, string cwd,
      OutputTypes output = OutputTypes.Verbose);
   Command Wrap(string fileName);
}
