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

Or If you are using the `npm`, add the below code to your `package.json` file to automatically install husky after the installation process:
```json
 "scripts": {
    "prepare": "dotnet tool restore && dotnet husky install"
 }
```

---

## Task runner

After installation, you must have `task-runner.json` file in your `.husky` directory that you can use to define your tasks.

you can run and test your tasks with `husky run` command. Once you are sure that your tasks are working properly, you can add it to the hook.

e.g.

```shell
dotnet husky add .husky/pre-commit "dotnet husky run"
```
<details>
<summary>A simple real-world example <code>task-runner.json</code></summary>
<p>

```json
{
   "tasks": [
      {
         "command": "dotnet",
         "group": "backend",
         "output": "verbose",
         "args": ["dotnet-format", "--include", "${staged}"],
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

<br>

### Task supported configurations

Using bellow configuration you can define your task with a lot of options.

---

| name      | optional |       type                | default  | description |
|---------- | -------- | ------                    | -------  | ----------- |
| command   |  false   | string                    |    -     | path to the executable file or script or executable name |
| args      |  true    | [string array]            |    -     | command arguments |
| include   |  true    | [array of glob]           |    **    | glob pattern to select files |
| name      |  true    | string                    |    -     | name of the task (recomended) |
| group     |  true    | string                    |    -     | group of the task |
| pathMode  |  true    | [absolute, relative]      | relative | file path style (releative or absolute) |
| cwd       |  true    | string                    | project root directory | current working directory for the command, can be relative or absolute
| output    |  true    | [always, error, verbose, never] | error | output log level |
| exclude   |  true    | [array of glob]           |    -     | glob pattern to exclude files |
| windows   |  true    | object                    |    -     | ovverides all the above settings for windows |

---

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
- Task `output` not showing errors correctly with default values. workarount -> setting output to `always`
