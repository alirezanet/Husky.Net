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
<Target Name="husky" AfterTargets="Restore" Condition="'$(HUSKY)' != 0"
        Inputs="../../.config/dotnet-tools.json"
        Outputs="../../.husky/_/install.stamp">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="../../" />  <!--Update this to the relative path to your project root dir -->
   <Touch Files="../../.husky/_/install.stamp" AlwaysCreate="true"
          Condition="Exists('../../.husky/_')" />
   <ItemGroup>
      <FileWrites Include="../../.husky/_/install.stamp" />
   </ItemGroup>
</Target>
```

::: tip
Make sure to update the working directory and the `Inputs`/`Outputs`/`Touch`/`FileWrites` paths depending on your folder structure. All paths should be relative to your project and point to the repository root dir.
:::

::: tip
The target uses MSBuild incremental build support (`Inputs`/`Outputs`) to avoid re-running on every build. It only re-runs when `.config/dotnet-tools.json` changes (e.g. tool version update) or after `dotnet clean`. The stamp file is created inside `.husky/_/` which is already gitignored.
:::

::: tip
For solutions with multiple projects, consider placing the target in a `Directory.Build.targets` file at the repository root. When placed at the root, you can replace relative paths (e.g. `../../`) with `$(MSBuildThisFileDirectory)` which resolves to the directory containing the targets file.
:::

::: warning
Adding the above code to a multiple targeted project will cause husky to run multiple times.
e.g
`<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`

to avoid this, you can add the `$(IsCrossTargetingBuild)' == 'true'` condition to the target.
e.g

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" AfterTargets="Restore" Condition="'$(HUSKY)' != 0 and '$(IsCrossTargetingBuild)' == 'true'">
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
