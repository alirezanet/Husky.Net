using System.IO.Abstractions;
using CliFx.Attributes;
using Husky.Services.Contracts;

namespace Husky.Cli;

[Command("hook rm", Description = "Remove a husky hook (alias: hook remove)")]
public class HookRmCommand : HookRemoveCommand
{
   public HookRmCommand(IGit git, IFileSystem fileSystem) : base(git, fileSystem) { }
}
