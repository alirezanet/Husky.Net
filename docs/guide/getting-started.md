
# Getting Started

Husky.Net is a dotnet tool available in nuget repository.

- [Husky](https://www.nuget.org/packages/Husky/)

## Installation

:::: code-group
::: code-group-item local (recommended)

```shell:no-line-numbers:no-v-pre
cd <Your project root directory>
dotnet new tool-manifest
dotnet tool install Husky
```

:::
::: code-group-item global

```shell:no-line-numbers:no-v-pre
dotnet tool install --global Husky
```

:::
::::

## Setup husky for your project

``` shell:no-line-numbers:no-v-pre
cd <Your project root directory>
dotnet husky install
```

::: tip
With the global installation, you don't need to add the `dotnet` prefix to the commands.
:::

## Husky and git submodules

The `install` command handles the hooks differently when it's running in a git submodule. The hooks are installed in the submodule's .git directory which is located in the `modules` folder of the super project's .git directory. Husky will alert you when it detects a submodule and tell you where it will attach the hooks:

```
Submodule detected, attaching .../Repository/Project/mySubmodule/.husky hooks to .../Repository/Project/.git/modules/mySubmodule
```

If you want to ignore the hooks when your project is a submodule call `install` with the `--ignore-submodule` option. This will make the `install` step a no-op. no git configurations will be applied and your hooks won't work. A message will alert you when this is happening:

```
Submodule detected and [--ignore-when-submodule] is set, skipping install target
```

## Add your first hook

``` shell:no-line-numbers:no-v-pre
dotnet husky add pre-commit -c "echo 'Husky.Net is awesome!'"
git add .husky/pre-commit
```

## Make a commit

``` shell:no-line-numbers:no-v-pre
git commit -m "Keep calm and commit"
# `echo 'Husky.Net is awesome!'` will run every time you commit
```
