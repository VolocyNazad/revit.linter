# Build and toolchain policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: changing the SDK, a build unit, a package version or a warning rule.

A build is reproducible from a clean checkout with the SDK selected by the repository and the package versions declared in the repository, including the Revit-year-bound packages that follow `$(MajorRevitVersion)`. It states rules, not build recipes: commands and stack facts belong to the [repository guide](../repository.md).

The supported Revit version set is declared by the `Release_*` configurations in the root `Directory.Build.props`. CI resolves its build matrix from that file through `scripts/Get-RevitConfigurations.ps1`, and `scripts/Validate-Repository.ps1` fails when the solution configurations or the testing-policy list drift from it.

## SDK selection

- Take the .NET SDK from the repository-root `global.json`; it is the only source of the SDK for local builds, CI and release publishing.
- Keep `global.json` on a stable SDK `10.0.103` or later in the `10.0` major/minor line, with `rollForward: latestFeature` and `allowPrerelease: false`. Do not move to another major/minor line without changing this policy and `global.json` in the same change.
- Read the SDK in every GitHub Actions setup step with `global-json-file: global.json` after checkout. Do not install a floating or explicitly versioned SDK in the workflow instead.
- Install additional SDKs only to supply runtimes for older test targets. They do not replace the SDK selected by `global.json` and must not be selected for the build.
- This policy does not change project target frameworks; a target-framework change updates the [repository guide](../repository.md) with the stack facts.

## Build units

- The root solution `Revit.Linter.slnx` builds the add-in and its tests. `installer/`, `build/`, `benchmark/`, `updater/` and `sandbox/` remain independent build units with their own solutions and their own responsibility; build each of them separately and do not expect the root build to cover them. Pull request CI also compiles the `build/` and `installer/` units; `benchmark/` and `sandbox/` stay local.
- Treat `updater/` as an independent build unit: it owns `updater/Directory.Build.props`, `updater/Directory.Packages.props` and `updater/Revit.Linter.Updater.slnx`, is absent from the root solution, and is not referenced by any root project. The add-in couples to it only by launching `Revit.Linter.Updater.exe` by path.
- Share product identity by compiling the `ProductIdentity` source file through a source link in independent build units instead of adding a project reference.
- Keep `netstandard2.0` projects Revit-independent: they must not reference the Revit API, WPF or the Revit SDK.
- A new build unit arrives with its own solution, its own `Directory.Build.props` and `Directory.Packages.props`, and its own CI job. A project that needs another build unit's files takes a source link or a package, never a reference across units.

## Central package management

- Pin every package version in the nearest `Directory.Packages.props`. The root file serves the root solution; `installer/`, `build/`, `benchmark/`, `sandbox/` and `updater/` have their own and use only those.
- Keep central package management enabled (`ManagePackageVersionsCentrally=true`); a `PackageReference` in a project carries no `Version`.
- Add repository-wide analyzer and generator packages through `GlobalPackageReference` with private assets, as `AutoConstructor`, `PolySharp` and `SonarAnalyzer.CSharp` are. Do not repeat them per project.
- Write Revit-versioned versions with `$(MajorRevitVersion)` and make them conditional on that property being defined.
- Change a package version in one place only. When a project resolves a version you did not expect, look for a nearer props file before editing the project.

## Revit version constants

- Use the SDK-defined constants: `BEFORE<year>` is true for `MajorRevitVersion <= year` and `AFTER<year>` for `MajorRevitVersion >= year`; both are inclusive.
- Write a boundary that starts at the year itself with `AFTER<year>` (`#if AFTER2024` / `#if !AFTER2024`).
- Keep conditional compilation inside compatibility extensions or adapters, not at call sites (see [architecture decisions](../architecture.md#module-boundaries)).
- Treat every `#if` branch as behaviour: it is verified on the configurations where it compiles, as the [testing policy](testing.md) requires.

## Warnings

- Treat compiler and analyzer warnings as delayed errors. Do not leave new warnings in code you touch; fix them in the same change.
- Suppress a deliberate false positive as narrowly as possible and add a nearby comment explaining why.
- Keep every release configuration warning-free. The release build compiles the add-in with `TreatWarningsAsErrors=true` (`build/Modules/CompileProjectModule.cs`), and `build-and-test` builds every `Release_*` configuration of the root solution with the same property, so a warning fails both the pull request and the publish.
- Do not rely on a local build to find warnings; build the configuration you touch before opening the pull request.

## Roslyn compatibility

- Target the centrally pinned `Microsoft.CodeAnalysis.CSharp` version for distributed analyzers and source generators.
- Keep Roslyn references private implementation dependencies.
- Change this baseline only with a passing run of the analyzer and generator tests on every supported configuration.

## MSBuild and SDK logic

- Test MSBuild and SDK logic on a temporary project, as the [testing policy](testing.md) requires; do not infer the behaviour from the product build alone.
- Keep shared build logic in the root `Directory.Build.props` and `Directory.Build.targets`. The updater repeats the settings it needs in its own props file instead of importing the root one.
- Build every solution affected by a shared MSBuild change: the root solution and the `updater/` solution at minimum, plus the build unit whose props, targets or SDK changed.
- Fix a broken build in the shared file rather than masking it with a local property change.

Version coverage and test placement are defined by the [testing policy](testing.md), publishing by the [release policy](releases.md), solution items by the [repository maintenance policy](repository-maintenance.md), and stack facts by the [repository guide](../repository.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
