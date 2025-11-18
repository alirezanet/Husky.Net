# Introduction

![Husky.Net WorkFlow](../.vuepress/public/workflow.jpg)

## Introduction

> Husky improves your commits and more 🐶 woof!
> Run linters against staged git files and don't let 💩 slip into your code base!

Inspired by the awesome **[husky](https://github.com/typicode/husky)** and **[lint-staged](https://github.com/okonet/lint-staged)** tools from the JS world, I built **Husky.Net** to bring the same power and simplicity to .NET developers — and then some! It lets you lint your commit messages, run tests, format or analyze code, and perform any other checks automatically when you commit or push. 🚀e!

## Features

- 🔥 It brings the **dev-dependency** concept to the .NET world!
- 🔥 Supports all Git & gitflow hooks
- 🔥 Internal task runner!
- Multiple file states (staged, last-commit, git-files, etc...)
- CSharp scripts (csx)
- Supports macOS, Linux and Windows
- Powered by modern new Git feature (core.hooksPath)
- User-define variables
- Compatible with [dotnet-format](https://github.com/dotnet/format), [CSharpier](https://csharpier.com/), [ReSharper command line tools](https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html) and other formatting tools
- User-friendly messages
- Git GUIs
- Custom directories
- Monorepo

-_A lot of features are coming soon, stay tuned! 👁️‍🗨️👀_

## Why use Hooks and Husky.Net?

As developers, we rely heavily on tools like GitHub, GitLab, Azure DevOps, and Atlassian to manage our code and collaborate efficiently. We take pride in writing clean, consistent code—and that’s why we keep adding new linters, formatters, and quality rules to our workflow. Ideally, every commit should be good enough to go straight to production. Nothing’s more annoying than commits like “fix build” or “fix lint errors.” Those small issues should be caught early, not after someone else pulls your changes or when the CI pipeline fails.

Imagine submitting your code for review while your CI job fails in the background—you’ll have to fix it, push again, and your reviewer might need to start over. That kind of back-and-forth wastes time for everyone.

Husky.Net makes it effortless to integrate git hooks, automate checks, and run your own scripts before bad commits ever reach your repository.
