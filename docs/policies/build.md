# Build and toolchain policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

A build is reproducible from a clean checkout with the SDK selected by the repository and the package versions pinned in the repository. This policy fixes the single source of the SDK, the independent build units, the central package and Revit-version conventions, and the warning and Roslyn baselines the other policies rely on. It states rules, not build recipes: commands and stack facts belong to the [repository guide](../repository.md).

## SDK selection

- Take the .NET SDK from the repository-root `global.json`; it is the only source of the SDK for local builds, CI and release publishing.
- Keep `global.json` on a stable SDK `10.0.103` or later in the `10.0` major/minor line, with `rollForward: latestFeature` and `allowPrerelease: false`. Do not move to another major/minor line without changing this policy and `global.json` in the same change.
- Read the SDK in every GitHub Actions setup step with `global-json-file: global.json` after checkout. Do not install a floating or explicitly versioned SDK in the workflow instead.
- Install additional SDKs only to supply runtimes for older test targets. They do not replace the SDK selected by `global.json` and must not be selected for the build.
- This policy does not change project target frameworks. A target-framework change is its own change and updates the [repository guide](../repository.md) with the stack facts.

## Build units

- The root solution `Revit.Linter.slnx` builds the add-in and its tests. `installer/`, `build/`, `benchmark/`, `updater/` and `sandbox/` remain independent build units with their own solutions and their own responsibility; build each of them separately and do not expect the root build to cover them.
- Treat `updater/` as an independent build unit: it owns `updater/Directory.Build.props`, `updater/Directory.Packages.props` and `updater/Revit.Linter.Updater.slnx`, uses `Microsoft.NET.Sdk` with plain `Debug`/`Release` configurations, is absent from the root solution, and is not referenced by any root project. The add-in couples to it only by launching `Revit.Linter.Updater.exe` by path.
- Share product identity by compiling the `ProductIdentity` source file through a source link in independent build units instead of adding a project reference. The shared file is the contract; a reference would couple the build units.
- Keep `netstandard2.0` projects Revit-independent: they are evaluated without a Revit configuration, so they must not reference the Revit API, WPF or the Revit SDK.
- A new build unit arrives with its own solution, its own `Directory.Build.props` and `Directory.Packages.props`, and its own CI job. A project that needs another build unit's files takes a source link or a package, never a reference across units.

## Central package management

- Pin every package version in the nearest `Directory.Packages.props`. The root file serves the root solution; `installer/`, `build/`, `benchmark/`, `sandbox/` and `updater/` have their own and use only those.
- Keep central package management enabled (`ManagePackageVersionsCentrally=true`); a `PackageReference` in a project carries no `Version`.
- Add repository-wide analyzer and generator packages through `GlobalPackageReference` with private assets, as `AutoConstructor`, `PolySharp` and `SonarAnalyzer.CSharp` are. Do not repeat them per project.
- Write Revit-versioned versions with `$(MajorRevitVersion)` and make them conditional on that property being defined, because the Revit-independent `netstandard2.0` projects are evaluated without a Revit configuration and an empty version fails the whole restore.
- Change a package version in one place only. When a project resolves a version you did not expect, look for a nearer props file before editing the project.

## Revit version constants

- Use the SDK-defined constants: `BEFORE<year>` is true for `MajorRevitVersion <= year` and `AFTER<year>` for `MajorRevitVersion >= year`; both are inclusive.
- Write a boundary that starts at the year itself with `AFTER<year>` (`#if AFTER2024` / `#if !AFTER2024`). A `BEFORE<year>` boundary would compile the older compatibility branch on Revit 2024 and trip the obsolete-API warnings the release build treats as errors.
- Keep conditional compilation inside compatibility extensions or adapters, not at call sites (see [architecture decisions](../architecture.md#module-boundaries)).
- Treat every `#if` branch as behaviour: it is verified on the configurations where it compiles, as the [testing policy](testing.md) requires.

## Warnings

- Treat compiler and analyzer warnings as delayed errors. Do not leave new warnings in code you touch; fix them in the same change.
- Suppress a deliberate false positive as narrowly as possible and add a nearby comment explaining why.
- Keep every release configuration warning-free. The release build compiles the add-in with `TreatWarningsAsErrors=true` (`build/Modules/CompileProjectModule.cs`), so a warning in a release configuration fails publishing.
- Do not rely on pull request CI to find release-only warnings: `build-and-test` builds the root and updater solutions in their release configurations without `-warnaserror`, while the `build/` and `installer/` solutions are compiled by the release publish. Verify the affected release configuration with a plain `dotnet build` before tagging.

## Roslyn compatibility

- Target the centrally pinned `Microsoft.CodeAnalysis.CSharp` version for distributed analyzers and source generators; it is the analyzer API of the Visual Studio version the repository builds against.
- Keep Roslyn references private implementation dependencies: they do not flow to package consumers and are not shipped as part of the product.
- Change this baseline only with a passing run of the analyzer and generator tests on every supported configuration. A Roslyn version is a compatibility contract, not a routine dependency update.

## MSBuild and SDK logic

- Test MSBuild and SDK logic on a temporary project, as the [testing policy](testing.md) requires; do not infer the behaviour from the product build alone.
- Keep shared build logic in the root `Directory.Build.props` and `Directory.Build.targets`. The updater repeats the settings it needs in its own props file instead of importing the root one.
- Build every solution affected by a shared MSBuild change: the root solution and the `updater/` solution at minimum, plus the build unit whose props, targets or SDK changed.
- Fix a broken build in the shared file rather than masking it with a local property change, so every build unit and CI see the same behaviour.

Version coverage and test placement are defined by the [testing policy](testing.md), publishing by the [release policy](releases.md), solution items and repository-level files by the [repository maintenance policy](repository-maintenance.md), and stack facts by the [repository guide](../repository.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
