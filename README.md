# Husky.Net

## Introduction

Husky improves your commits and more 🐶 woof!

You can use it to lint your commit messages, run tests, lint code, etc... when you commit or push.

**Features**

-  Supports all Git hooks
-  Powered by modern new Git feature (core.hooksPath)
-  User-friendly messages
-  Supports macOS, Linux and Windows
-  Git GUIs
-  Custom directories
-  Monorepo
-  🔥 Internal task runner! 🆕
-  🔥 Multiple file states (staged, lastCommit, glob) 🆕
-  🔥 Compatible with [dotnet-format](https://github.com/dotnet/format) 🆕
-  🔥 Customizable tasks 🆕

_next_

-  ⌛ Task for specific branch or tags (soon)
-  ⌛ User-defined file states (soon)
-  ⌛ Internal commit-msg linter (soon)

If you already know what is the lint-staged or Husky (npm packages),
this is very similar but you can use Husky.Net without having node, yarn, etc.. installed, with a lot of more features! 🚀🚀

### A lot of features are coming soon, stay tuned! 👁️‍🗨️👀

## Installation

```shell
# local installation (recommended)
cd <Your project root directory>
dotnet new tool-manifest
dotnet tool install Husky

# global installation
dotnet tool install --global Husky

```
_**Note**: With the global installation, you don't need to add the `dotnet` prefix to the commands._

### Setup husky for your project

```shell
cd <Your project root directory>
dotnet husky install
```

### Add your first hook

```shell
dotnet husky add .husky/pre-commit "echo 'Husky is awesome!'"
git add .husky/pre-commit
```

### Make a commit

```shell
git commit -m "Keep calm and commit"
# `echo 'Husky is awesome!'` will run every time you commit
```

## Automate husky installation for other contributors

If you installed husky locally, just add the below code to **one** of your projects (*.csproj *.vbproj).

**Important:** Just make sure to update the **working directory** depending on your folder structure.

```xml
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High" 
         WorkingDirectory="../../" />  <!--Update this to the releative path to your project root dir --> 
</Target>
```


If you have only one multiple target project (`TargetFrameworks`) add the bellow condition `IsCrossTargetingBuild` to the target tag to prevent multiple execution
```xml
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(IsCrossTargetingBuild)' == 'true'"> 
   ...
```
---

## Task runner

After installation, you must have `task-runner.json` file in your `.husky` directory that you can use to define your tasks.

you can run and test your tasks with `husky run` command.

to use tasks in your git hooks, you can use `husky run` command.

e.g.

```shell
dotnet husky add .husky/pre-commit "husky run"
```
<details>
<summary>A simple real-world example <code>task-runner.json</code></summary>
<p>

```json
{
   {
   "tasks": [
      {
         "command": "dotnet-format",
         "group": "backend",
         "args": ["--include", "${staged}"],
         "include": ["**/*.cs", "**/*.vb"]
      },
      {
         "name": "eslint",
         "group": "frontend",
         "command": "npm",
         "pathMode": "absolute",
         "cwd": "Client",
         "args": ["run", "lint", "${staged}"],
         "include": ["**/*.ts", "**/*.vue", "**/*.js"]
      },
      {
         "name": "prettier",
         "group": "frontend",
         "command": "npx",
         "pathMode": "absolute",
         "cwd": "Client",
         "args": ["prettier", "--write", "--ignore-unknown", "${staged}"],
         "include": [
            "**/*.ts",
            "**/*.vue",
            "**/*.js",
            "**/*.json",
            "**/*.yml",
            "**/*.css",
            "**/*.scss"
         ]
      },
      {
         "name": "Welcome",
         "output": "always",
         "command": "bash",
         "args": ["-c", "echo  🌈 Nice work! 🥂"],
         "windows": {
            "command": "cmd",
            "args": ["/c", "echo  🌈 Nice work! 🥂"]
         }
      }
   ]
}
 
```

</p>
</details>  

## Glob patterns

Husky.Net supports the standard dotnet `FileSystemGlobbing` patterns for include or exclude task configurations. read more [here](https://docs.microsoft.com/en-us/dotnet/core/extensions/file-globbing#pattern-formats)

---


## Notes

- I've added two sample task to the `task-runner.json` file,
make sure to read the comments before removing them until we complete the documentation. **any help appreciated!**

- Consider all bellow 1.x versions as beta. ( we need a lot of tests before major release )

- Don't forget to give a ⭐ on [GitHub](https://github.com/alirezanet/husky.net)

- This library inspired and is a combination of [husky](https://github.com/typicode/husky) & [lint-staged](https://github.com/okonet/lint-staged) & VsCode Task runner!, for **DotNet**, so make sure to support them too!

## Known issues
- `husky run` command doesn't have color when executed from hooks.
