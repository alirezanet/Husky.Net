
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
