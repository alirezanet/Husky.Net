# Using C# code in your git hooks

You can use task runner `exec` command to execute a C# script.

e.g

``` shell
dotnet husky exec <csx-file-path>
# e.g
# dotnet husky exec .husky/csx/hello.csx
```

Also, you can use your csx scripts in your tasks.

``` json
{
   "command": "dotnet",
   "args": [ "husky", "exec", ".husky/csx/hello.csx"  ],
}
```

## Examples

### Simple commit message linter

This repo is using a csharp script to lint the commit messages, you can check it here:

[commit-lint.csx](../../.husky/csx/commit-lint.csx)
@[code{7-} csharp](../../.husky/csx/commit-lint.csx)

[commit-msg *hook*](../../.husky/commit-msg)
@[code shell](../../.husky/commit-msg)

[task-runner.json](../../.husky/task-runner.json)
@[code{9-14} json](../../.husky/task-runner.json)
