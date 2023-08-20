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

## Disable husky in submodule

If your project will be used as a git submodule, and you don't want the hooks for it to be attached. Call the `attach` command with the `--ignore-submodule` option. see [Manual Attach](#manual-attach) section for more details on how this should look like in your `csproj`.

## Manual Attach

To manually attach husky to your project, add the below code to one of your projects (*.csproj/*.vbproj).

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="../../" />  <!--Update this to the relative path to your project root dir -->
</Target>
```

To skip running the target when the project is a .git submodule, update the condition for your MsBuild target to also check for the `IgnoreSubmodule` variable. The target will then look like this:

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0  and '$(IgnoreSubmodule)' != 0">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install --ignore-submodule" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="../../" />  <!--Update this to the relative path to your project root dir -->
</Target>
```

::: tip
Make sure to update the working directory depending on your folder structure it should be a relative path to your project root dir
:::

::: warning
Adding the above code to a multiple targeted project will cause husky to run multiple times.
e.g
`<TargetFrameworks>netcoreapp3.1;net5.0;net6.0;net7.0</TargetFrameworks>`

to avoid this, you can add the `$(IsCrossTargetingBuild)' == 'true'` condition to the target.
e.g

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0 and '$(IsCrossTargetingBuild)' == 'true'">
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
