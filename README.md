# Husky.Net

## Introduction

You can use Husky.Net without having node, yarn, etc.. installed! 🚀🚀

Husky improves your commits and more 🐶 woof!

You can use it to lint your commit messages, run tests, lint code, etc... when you commit or push. Husky supports all Git hooks.

**Features**
- Powered by modern new Git feature (core.hooksPath)
- User-friendly messages
- Supports
- macOS, Linux and Windows
- Git GUIs
- Custom directories
- Monorepos

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

### Wanna support us?

Don't forget to give us a ⭐ on [GitHub](https://github.com/alirezanet/husky.net)

This library inspired and is a combination of [husky](https://github.com/typicode/husky) & [lint-staged](#) libraries for **DotNet**, so make sure to support them too!

### A lot of features are coming soon, so stay tuned! 👁️‍🗨️👀
