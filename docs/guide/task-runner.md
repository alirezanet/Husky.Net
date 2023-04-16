# Task Runner

## Why task runner?

Linting makes more sense when run before committing your code. By doing so you can ensure no errors go into the repository and enforce code style. But running a lint process on a whole project is slow, and linting results can be irrelevant. Ultimately you only want to lint files that will be committed.

## task-runner.json

After installation, you must have a `task-runner.json` file in your `.husky` directory that you can use to define your tasks.

you can run and test your tasks with `dotnet husky run` command. Once you are sure that your tasks are working properly, you can add it to the hook.

e.g

``` shell:no-line-numbers:no-v-pre
stages_files=$(git diff --staged --name-only --no-ext-diff --diff-filter=AM | tr '\n' ';' | sed 's/;$//')

dotnet husky add pre-commit -c "dotnet husky run" --args $stages_files
```

::: details A real-world example.

``` json
{
   "tasks": [
      {
         "name": "dotnet-format",
         "group": "pre-commit",
         "command": "dotnet",
         "args": ["dotnet-format", "--include", "${staged}"],
         "include": ["**/*.cs", "**/*.vb"]
      },
      {
         "name": "commit-message-linter",
         "command": "dotnet",
         "args": [
            "husky",
            "exec",
            ".husky/csx/commit-lint.csx",
            "--args",
            "${args}"
         ]
      },
      {
         "name": "warning-check",
         "command": "dotnet",
         "group": "pre-push",
         "args": ["build", "/warnaserror"],
         "include": ["**/*.cs", "**/*.vb"]
      },
      {
         "name": "eslint",
         "group": "pre-commit",
         "pathMode": "absolute",
         "cwd": "Client",
         "command": "npm",
         "args": ["run", "lint", "${staged}"],
         "include": ["**/*.ts", "**/*.vue", "**/*.js"]
      },
      {
         "name": "prettier",
         "group": "pre-commit",
         "pathMode": "absolute",
         "cwd": "Client",
         "command": "npx",
         "args": ["prettier", "--write", "${staged}"],
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
         "args": ["-c", "echo Nice work! 🥂"],
         "windows": {
            "command": "cmd",
            "args": ["/c", "echo Nice work! 🥂"]
         }
      },
      {
         "name": "Run JB Clean Up Code",
         "command": "cmd",
         "pathMode": "relative",
         "args": [
           "/c",
           "dotnet",
           "jb",
           "cleanupcode",
           "proj.sln",
           "--profile=Team: Full Cleanup",
           "--include=${args}"
         ],
         "group": "pre-commit"
      }
   ]
}
```

:::
