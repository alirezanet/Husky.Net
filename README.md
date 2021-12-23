# Husky.Net

## Introduction

Husky improves your commits and more 🐶 woof!

You can use it to lint your commit messages, run tests, lint code, etc... when you commit or push.

**Features**

_v0.0.1_

-  Supports all Git hooks
-  Powered by modern new Git feature (core.hooksPath)
-  User-friendly messages
-  Supports macOS, Linux and Windows
-  Git GUIs
-  Custom directories
-  Monorepo

_v0.0.2_

-  🔥 Internal task runner! 🆕
-  🔥 Multiple file states (staged, lastCommit, glob) 🆕
-  🔥 Compatible with [dotnet-format](https://github.com/dotnet/format) 🆕
-  🔥 Customizable tasks 🆕

_next_

-  ⌛ Task for specific branch or tags (soon)
-  ⌛ User-defined file states (soon)

If you already know what is the lint-staged or Husky (npm packages),
this is very similar but you can use Husky.Net without having node, yarn, etc.. installed, with a lot of more features! 🚀🚀

### A lot of features are coming soon, stay tuned! 👁️‍🗨️👀

## Installation

```shell
# global installation
dotnet tool install --global Husky

# local installation
cd <Your project root directory>
dotnet new tool-manifest
dotnet tool install Husky
```

_Note: With the local installation, you have to prefix the commands with `dotnet` e.g `dotnet husky`_

### Setup husky for your project

```shell
cd <Your project root directory>
husky install
```

### Add your first hook

```shell
husky add .husky/pre-commit "echo 'Husky is awesome!'"
git add .husky/pre-commit
```

### Make a commit

```shell
git commit -m "Keep calm and commit"
# `echo 'Husky is awesome!'` will run every time you commit
```

---

## Task runner

After installation, you must have `task-runner.json` file in your `.husky` directory that you can use to define your tasks.

you can run and test your tasks with `husky run` command.

to use tasks in your git hooks, you can use `husky run` command.

e.g.

```shell
husky add .husky/pre-commit "husky run"
```

## Glob patterns

Husky.Net supports the standard dotnet `FileSystemGlobbing` patterns for include or exclude task configurations. read more [here](https://docs.microsoft.com/en-us/dotnet/core/extensions/file-globbing)

---

## Notes

I've added two sample task to the `task-runner.json` file,
make sure to read the comments before removing them until we complete the documentation. **any help appreciated!**

Don't forget to give a ⭐ on [GitHub](https://github.com/alirezanet/husky.net)

This library inspired and is a combination of [husky](https://github.com/typicode/husky) & [lint-staged](https://github.com/okonet/lint-staged) & VsCode Task runner!, for **DotNet**, so make sure to support them too!
