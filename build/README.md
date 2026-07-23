# Build pipeline

The separate `Revit.Linter.Build.slnx` solution contains the build pipeline. It compiles
Revit.Linter for every Revit version configured in `appsettings.json` and
creates one MSI per version.

Build the pipeline solution itself:

```powershell
dotnet build build/Revit.Linter.Build.slnx -c Release
```

Build all configured add-in versions:

```powershell
dotnet run --project build/Revit.Linter.Build.csproj -c Release
```

Build all configured add-in versions and create the installers:

```powershell
dotnet run --project build/Revit.Linter.Build.csproj -c Release -- pack
```

Installers are written to the configured `Build:OutputDirectory` (`output` by
default). The product version is calculated by GitVersion from Git tags. Create and
checkout an exact stable tag before producing release installers:

```powershell
git tag v1.4.0
git switch --detach v1.4.0
dotnet run --project build/Revit.Linter.Build.csproj -c Release -- pack
```

GitHub releases are created manually by the `Publish release` workflow. Tag the
current commit (for example, `v1.4.0`), push the commit and tag, and run the
workflow for that branch. GitVersion resolves the version from the tag; the
pipeline creates all MSI files and publishes them as release assets.

The workflow delegates the complete release to ModularPipelines. The equivalent
local command (requires authenticated GitHub CLI) is:

```powershell
dotnet run --project build/Revit.Linter.Build.csproj -c Release -- publish
```

Inspect the version resolved by GitVersion without compiling the add-in:

```powershell
dotnet run --project build/Revit.Linter.Build.csproj -c Release --no-launch-profile -- version
```

To support another Revit version, add its build configuration and target
framework to `Build:Versions` in `appsettings.json`. The installer generator
already contains stable upgrade codes for Revit 2021 through 2027.
