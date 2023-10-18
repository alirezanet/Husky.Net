# Husky.Net

![GitHub](https://img.shields.io/github/license/alirezanet/husky.net) ![Nuget](https://img.shields.io/nuget/dt/husky?color=%239100ff) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/husky?label=latest) ![Nuget](https://img.shields.io/nuget/v/husky?label=stable) ![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/alirezanet/Husky.Net/build.yml?branch=master?label=checks) 
[![NuGet version (Husky)](https://img.shields.io/nuget/v/Husky.svg?style=flat-square)](https://www.nuget.org/packages/Husky/)

![Husky.Net WorkFlow](https://github.com/alirezanet/Husky.Net/blob/master/docs/.vuepress/public/workflow.jpg)

## Introduction

> Husky improves your commits and more 🐶 woof!
>
> Run linters against staged git files and don't let 💩 slip into your code base!

These two quotes from husky and lint-staged JS tools inspired me to create Husky.Net for dotnet developers. it provides a simple native way to do both also has a lot of other cool features. You can use it to lint your commit messages, run tests, lint/format code, etc... when you commit or push. 🚀🚀

## Features

- 🔥 It brings the **dev-dependency** concept to the .NET world!
- 🔥 Internal task runner!
- 🔥 Supports all Git & gitflow hooks
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

## Documentation

- [Get started](https://alirezanet.github.io/Husky.Net/guide/getting-started)
- [Documentation](https://alirezanet.github.io/Husky.Net)

## Support

- Don't forget to give a ⭐ on [GitHub](https://github.com/alirezanet/husky.net)
- Share your feedback and ideas to improve this tool
- Share Husky.Net on your favorite social media and your friends
- Write a blog post about Husky.Net

## Articles / Examples
- [Pre-commit hooks with Husky.NET - build, format, and test your .NET application before a Git commit](https://www.code4it.dev/blog/husky-dotnet-precommit-hooks/)
- [Automatically version and release .Net Application](https://blog.antosubash.com/posts/automatic-version-and-release) by [@antosubash](https://github.com/antosubash)
- [<span dir="rtl" align="right">چرا باید از Git Hooks استفاده کنیم؟ معرفی Husky.Net</span>](https://www.dntips.ir/post/3367/%da%86%d8%b1%d8%a7-%d8%a8%d8%a7%db%8c%d8%af-%d8%a7%d8%b2-git-hooks-%d8%a7%d8%b3%d8%aa%d9%81%d8%a7%d8%af%d9%87-%da%a9%d9%86%db%8c%d9%85-%d9%85%d8%b9%d8%b1%d9%81%db%8c-husky-net)
- Comming soon

## Contribution

Feel free to send me a pull request!

Check out the [Contribution Page](https://alirezanet.github.io/Husky.Net/contribution)

## Credits

- This tool inspired of [husky](https://github.com/typicode/husky) & [lint-staged](https://github.com/okonet/lint-staged) and a few other tools, for **DotNet**, so make sure to support them too!

- I'd also like to thank [@kaylumah](https://github.com/kaylumah) for his [article](https://kaylumah.nl/2019/09/07/using-csharp-code-your-git-hooks.html) that gave me the csharp scripting support idea.

## License

[MIT](https://github.com/alirezanet/husky.net/blob/master/LICENSE)
