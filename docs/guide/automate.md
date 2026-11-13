# Automate installation for other contributors

Husky.Net brings the **dev-dependency** concept to the .NET ecosystem.

You can attach husky to your project without adding extra dependencies! This way the other contributors will use your pre-configured tasks automatically.

## Attach Husky to your project

To attach Husky to your project, you can use the following command:

```shell
dotnet husky attach <path-to-project-file>
```

This will add the required configuration to your project file.

check out the [Manual Attach](#manual-attach) section for more details.

## Disable husky in CI/CD pipelines

You can set the `HUSKY` environment variable to `0` in order to disable husky in CI/CD pipelines.

## Manual Attach

To manually attach husky to your project, add the below code to one of your projects (*.csproj/*.vbproj).

``` xml:no-line-numbers:no-v-pre
<PropertyGroup>
   <!-- Update this to the relative path from your project to the repo root -->
   <HuskyRoot Condition="'$(HuskyRoot)' == ''">../../</HuskyRoot>
</PropertyGroup>
<Target Name="Husky" AfterTargets="Restore"
        Condition="'$(HUSKY)' != 0 and !Exists('$(HuskyRoot).husky/_/install.stamp')">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="$(HuskyRoot)" />
   <Touch Files="$(HuskyRoot).husky/_/install.stamp" AlwaysCreate="true"
          Condition="Exists('$(HuskyRoot).husky/_')" />
</Target>
<Target Name="HuskyClean" AfterTargets="Clean">
   <Delete Files="$(HuskyRoot).husky/_/install.stamp" />
</Target>
```

::: tip
Update the `HuskyRoot` property value to match the relative path from your project to the repository root directory (with a trailing slash). All other paths derive from it automatically.
:::

::: tip
The `Husky` target skips when `.husky/_/install.stamp` exists, avoiding re-runs on every build. The `HuskyClean` target removes the stamp on `dotnet clean`, so the next build re-installs. If you update your tool versions, run `dotnet clean` to pick up the change.
:::

::: tip
For solutions with multiple projects, consider placing the target in a `Directory.Build.targets` file at the repository root. When placed at the root, set `HuskyRoot` to `$(MSBuildThisFileDirectory)` and all paths resolve automatically with no manual configuration. Do not use `Directory.Build.props` for this; targets belong in `.targets` files.
:::

::: warning
Adding the above code to a multiple targeted project will cause husky to run multiple times.
e.g
`<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`

to avoid this, you can add the `$(IsCrossTargetingBuild)' == 'true'` condition to the target.
e.g

``` xml:no-line-numbers:no-v-pre
<Target Name="Husky" AfterTargets="Restore" Condition="'$(HUSKY)' != 0 and '$(IsCrossTargetingBuild)' == 'true'">
...
```

:::

## package.json alternative

If you are using the npm, add the below code to your package.json file will automatically install husky after the npm install

``` json
 "scripts": {
      "prepare": "dotnet tool restore && dotnet husky install"
 }
```
