using System.IO.Abstractions;
using CliFx.Attributes;
using Husky.Services.Contracts;

namespace Husky.Cli;

[Command("hook ls", Description = "List all husky hooks (alias: hook list)")]
public class HookLsCommand : HookListCommand
{
   public HookLsCommand(IGit git, IFileSystem fileSystem) : base(git, fileSystem) { }
}
