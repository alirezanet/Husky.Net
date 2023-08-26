# Git submodules

Husky handles git submodules in two ways:

- The project hooks and husky steps are completely ignored when the project is a git submodule.
- The hooks are installed in the submodule's git directory. This is the default mode following the step in [Installation](./getting-started.md/#installation) and [Attach Husky to your project](./automate.md/#attach-husky-to-your-project) should get you up and running.

## Installing husky

When running `dotnet husky install` Husky will alert you when it detects a submodule and tell you where it will attach the hooks:

```:no-line-numbers
Submodule detected, attaching .../Repository/Project/mySubmodule/.husky hooks to .../Repository/Project/.git/modules/mySubmodule
```

::: warning
The submodule hooks will only be executed when you're issuing git commands from inside the submodule folder.
:::

### Ignoring submodule install

For Husky to ignore installing when in a submodule, call `dotnet husky install --ignore-submodule`. This will make the install step a no-op. No git configurations will be applied and your hooks won't be attached.

A message will alert you when this is happening:

```:no-line-numbers
Submodule detected and [--ignore-when-submodule] is set, skipping install target
```

## Attaching husky

The `attach` command offers a `--ignore-submodule` options that generates an MsBuild target you can skip by setting the `IgnoreSubmodule` variable to `0` similar to the `Husky` variable, see [Disable husky in CI/CD pipelines](./automate.md#disable-husky-in-ci-cd-pipelines)

The generated block will look something like this, If you're attaching husky manually copy the target to your `.csproj` and adjust `WorkingDirectory` accordingly.

```xml:no-line-numbers:no-v-pre
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != 0  and '$(IgnoreSubmodule)' != 0">
   <Exec Command="dotnet tool restore"  StandardOutputImportance="Low" StandardErrorImportance="High"/>
   <Exec Command="dotnet husky install --ignore-submodule" StandardOutputImportance="Low" StandardErrorImportance="High"
         WorkingDirectory="../../" />  <!--Update this to the relative path to your project root dir -->
</Target>
```

::: tip
If you want your submodule hooks ignored but still want the MsBuild target to run, remove the `and '$(IgnoreSubmodule)' != 0` condition. `dotnet husky install --ignore-submodule` is enough to prevent the installation of the hooks.
:::