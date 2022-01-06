# Automate installation for other contributors

There are many ways to automate husky installation for everyone. below you can see a few examples.

::: tip
If you want to automate husky for other contributors, you should use local installation.
:::

## Using project files (recommended)

You can add the below code to one of your projects (*.csproj/*.vbproj).

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="../../" />  <!--Update this to the relative path to your project root dir -->
</Target>
```

::: tip
Make sure to update the working directory depending on your folder structure it should be a relative path to your project root dir
:::

::: warning
Adding the above code to a multiple targeted project will cause husky to run multiple times.
e.g
`<TargetFrameworks>netcoreapp3.1;net5.0;net6.0</TargetFrameworks>`

to avoid this, you can add the `$(IsCrossTargetingBuild)' == 'true'` condition to the target.
e.g

``` xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0 and '$(IsCrossTargetingBuild)' == 'true'">
...
```

:::

## Disable husky in CI/CD pipelines

You can set the `HUSKY` environment variable to `0` in order to disable husky in CI/CD pipelines.

## Using package.json

If you are using the npm, add the below code to your package.json file will automatically install husky after the npm install

``` json
 "scripts": {
    "prepare": "dotnet tool restore && dotnet husky install"
 }
 ```
