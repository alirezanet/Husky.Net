# Husky.Net

## Introduction

Husky improves your commits and more 🐶 woof!

You can use it to lint your commit messages, run tests, lint code, etc... when you commit or push. Husky supports all Git hooks.

**Features**
- Powered by modern new Git feature (core.hooksPath)
- User-friendly messages
- Supports macOS, Linux and Windows
- Git GUIs
- Custom directories
- Monorepos
- Staged-hooks! (soon)
- [dotnet-format](https://github.com/dotnet/format) Intergration (soon)

If you already know what is the Husky (npm library), this is very similar but you can use Husky.Net without having node, yarn, etc.. installed with a lot of more features! 🚀🚀

## Installation

```shell
dotnet tool install --global Husky
```

## Setup husky for your project

```shell
cd <Your project root directory> # <-- important
husky install
```

## Add your first hook

```shell
husky add .husky/pre-commit "echo 'Husky is awesome!'"
```

### Notes

Don't forget to give us a ⭐ on [GitHub](https://github.com/alirezanet/husky.net)

This library inspired and is a combination of [husky](https://github.com/typicode/husky) & [lint-staged](https://github.com/okonet/lint-staged) libraries for **DotNet**, so make sure to support them too!

### A lot of features are coming soon, so stay tuned! 👁️‍🗨️👀

