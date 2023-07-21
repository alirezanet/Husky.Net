# Configuration

Each task in `task-runner.json` is a JSON object with the following properties:

| name                        | optional | type                     | default                | description                                                            |
| --------------------------- | -------- | ------------------------ | ---------------------- | ---------------------------------------------------------------------- |
| command                     | false    | string                   | -                      | path to the executable file or script or executable name               |
| args                        | true     | [string array]           | -                      | command arguments                                                      |
| include                     | true     | [array of glob]          | `**/*`                 | glob pattern to select files                                           |
| name                        | true     | string                   | command                | name of the task (recommended)                                         |
| group                       | true     | string                   | -                      | group of the task (usually it should be the hook name)                 |
| branch                      | true     | string (regex)           | -                      | run task on specific branches only                                     |
| pathMode                    | true     | [absolute, relative]     | relative               | file path style (relative or absolute)                                 |
| cwd                         | true     | string                   | project root directory | current working directory for the command, can be relative or absolute |
| output                      | true     | [always, verbose, never] | always                 | output log level                                                       |
| exclude                     | true     | [array of glob]          | -                      | glob pattern to exclude files                                          |
| windows                     | true     | object                   | -                      | overrides all the above settings for windows                           |
| skipAutoStage               | true     | bool                     | false                  | Re-staging staged files                                                |
| ignoreValidateCommandResult | true     | bool                     | false                  | Ignores validate command result                                        |

## Glob patterns

Husky.Net supports the standard dotnet `FileSystemGlobbing` patterns for include or exclude task configurations. The patterns that are specified in the `include` and `exclude` can use the following formats to match multiple files or directories.

- Exact directory or file name
  - some-file.txt
  - path/to/file.txt
- Wildcards * in file and directory names that represent zero to many characters not including separator characters.

| Value        | Description                                                            |
| ------------ | ---------------------------------------------------------------------- |
| *.txt        | All files with .txt file extension.                                    |
| *.*          | All files with an extension.                                           |
| *            | All files in top-level directory.                                      |
| .*           | File names beginning with '.'.                                         |
| *word*       | All files with 'word' in the filename.                                 |
| readme.*     | All files named 'readme' with any file extension.                      |
| styles/*.css | All files with extension '.css' in the directory 'styles/'.            |
| scripts/*/*  | All files in 'scripts/' or one level of subdirectory under 'scripts/'. |
| images*/*    | All files in a folder with name that is or begins with 'images'.       |

- Arbitrary directory depth (/**/).

| Value    | Description                                 |
| -------- | ------------------------------------------- |
| **/*     | All files in any subdirectory.              |
| dir/**/* | All files in any subdirectory under 'dir/'. |

- Relative paths.

To match all files in a directory named "shared" at the sibling level to the base directory use `../shared/*`.

[Read more here](https://docs.microsoft.com/en-us/dotnet/core/extensions/file-globbing#pattern-formats)

## Variables

There are some variables that you can use in your task arguments (`args`).

- **${staged}**
  - returns the list of currently staged files
- **${last-commit}**
  - returns last commit changed files
- **${git-files}**
  - returns the output of (git ls-files)
- **${all-files}**
  - returns the list of matched files using include/exclude, be careful with this variable, it will return all the files if you don't specify include or exclude
- **${args}**
  - returns the arguments passed directly to the `husky run` command using `--args` option

e.g.

``` json:no-line-numbers:no-v-pre
"args": [ "${staged}" ]
```

### Custom variables

You can define your own variables by adding a task to the `variables` section in `task-runner.json`.

e.g.

defining custom `${root-dir-files}` variable to access root directory files

``` json
{
   "variables": [
      {
         "name": "root-dir-files",
         "command": "cmd",
         "args": ["/c", "dir", "/b"]
      }
   ],
   "tasks": [
      {
         "command": "cmd",
         "args": ["/c", "echo", "${root-dir-files}"]
      }
   ]
}
```
