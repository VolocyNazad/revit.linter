# Release policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

A release turns a tagged commit into a GitHub release with installers: it selects a channel, fixes the version, prepares the changelog and publishes the MSI through the build pipeline. The workflow is `.github/workflows/publish-release.yml` and the publish mechanics are the modules under `build/`; the tag and the release section of `CHANGELOG.md` are the inputs the pipeline consumes. The [changelog policy](changelog.md) owns the section format and its composition, the [branching policy](branching.md) owns release and hotfix branches, and the [testing policy](testing.md) owns what must be green before the tag.

## Release channels

The product has two release channels that share one repository, one changelog and one build pipeline.

- A **stable release** is a `vMAJOR.MINOR.PATCH` tag. It is the only channel the updater offers. Publish its GitHub Release without the **Set as a pre-release** flag so `/releases/latest` returns it and existing users can discover it; the build pipeline creates the release without that flag.
- A **pre-release** is a `vMAJOR.MINOR.PATCH-<channel>.<number>` tag, for example `-beta.1` or `-rc.1`. Use it when a build should reach testers before the stable release: a risky migration, a new installer or a large diagnostic change that benefits from real-model feedback. Publish it with the **Set as a pre-release** flag. Pre-releases are downloaded from the Releases page by people who opt in; the updater never announces them, and GitHub does not treat them as `latest`.

The updater discovers releases through `/releases/latest`, which returns only the latest published release that is neither a draft nor a pre-release, and the client rejects draft and pre-release payloads. Only a newer stable version can produce an update, so a stable release carrying the pre-release flag is invisible to existing users even though its tag and installer exist.

Current automation publishes stable releases only. The release pipeline requires `GitVersion` to produce a stable version and invokes `gh release create` without `--prerelease`. Publish a pre-release manually with `gh release create vX.Y.Z-beta.N --prerelease ...`, or extend the pipeline first. Do not give a pre-release a stable version number.

## Versioning

- `GitVersion` runs in Continuous Delivery mode with patch increments. The tag is the version source: a release commit carries exactly one stable `vMAJOR.MINOR.PATCH` tag.
- The build uses the tagged version without the `v` prefix for the binaries and the MSI. The MSI product version is the numeric `MAJOR.MINOR.PATCH`; a suffix-bearing version is reduced to those three numbers, while the add-in and updater assemblies keep the full `GitVersion` semantic version.
- The repository publishes a GitHub release with installers instead of a NuGet package.
- The tagged commit already contains the release section in `CHANGELOG.md` with an empty `## [Unreleased]` above it; the pipeline publishes that section as the GitHub release description. Do not compose or edit that section outside the [changelog policy](changelog.md).

## Preparing a release

Before creating the tag:

- Confirm the full Revit test matrix is executed and recorded in the release pull request, as the [testing policy](testing.md) requires.
- Confirm the build is green for every supported `Release_*` configuration, the working tree is clean and the release changelog section is committed.
- Verify the release tag and version against the release commit.
- Keep every release configuration warning-free and verify with a plain `dotnet build` before tagging; the publish compiles the add-in with `TreatWarningsAsErrors=true`, so a warning fails the publish.
- Confirm CI succeeds before the release pipeline runs.

Cut and prepare the release branch as the [branching policy](branching.md) requires; its branch and merge steps are not repeated here.

## Release metadata rules

- Never mark a stable version as a pre-release. That hides it from `/releases/latest` and from the updater, leaving users on the previous stable release even though the tag and installer exist.
- Never give a pre-release a stable version number. Use `vX.Y.Z-beta.N`; `vX.Y.Z` is reserved for the stable release that follows it.
- Never delete, move or force-push a pushed release tag. A pushed tag is immutable; publish the next version instead. If a mistake is found before the tag is pushed and no release was published, fix the commit, move the local tag and push it once.
- If a stable release was published with the pre-release flag by mistake, correct the GitHub release metadata by unchecking the flag; never move or delete the tag. Keep its assets: users may have downloaded them, and the release page and changelog link to them.
- Only the stable release has a `CHANGELOG.md` version section. Pre-release notes may summarize the changes for testers, but they do not create a permanent changelog section.

## Release artifacts and MSI

- Publish the updater and the Windows App SDK notification runtime for `win-x64` as a self-contained unpackaged application before MSI creation. The publish directory is an explicit build-module result; installer generation must consume that result instead of configuration-specific paths under `bin`.
- Verify the ILRepack output and the localization satellites before creating the MSI.
- Produce one per-user MSI containing every supported Revit build and the shared updater. It installs under `%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, creates version-specific manifests under `%APPDATA%\Autodesk\Revit\Addins\<year>\`, and registers the updater under the current user's `Run` key.
- Keep one stable `UpgradeCode`; the product code changes deterministically with the stable product version. The first unified installation detects and removes older per-Revit-version products by their legacy upgrade codes.
- Expose one required `Core` feature and one optional feature per supported Revit year. Preselect installed years through the `ProductName` lookup under `HKLM\SOFTWARE\Autodesk\Revit\Autodesk Revit <year>\Components`, and let the feature-tree UI override that selection. Synchronize the manifests with the requested feature states on install, repair and modify.
- Before changing any installed file, stop the short-lived updater and refuse to continue while a Revit process is running.
- Publish the installer asset as `RevitLinter-<version>.msi`, the name defined by `ProductIdentity.GetMsiAssetName`. The updater accepts a release installer only when its GitHub asset name and HTTPS URL exactly match the stable release version.

## Publish automation

`.github/workflows/publish-release.yml` is the release workflow. It runs on `workflow_dispatch` with a boolean `draft` input (default `false`); a tag push alone does not trigger it.

The workflow:

- checks out with full history, installs the SDK from `global.json` and installs WiX 6.0.2;
- verifies that `HEAD` carries exactly one stable tag matching `vMAJOR.MINOR.PATCH`, that `CHANGELOG.md` has a `## [Unreleased]` section with no entries below it, and that the changelog contains exactly one `## [VERSION] - YYYY-MM-DD` section for the tag;
- runs `dotnet run --project build/Revit.Linter.Build.csproj -c Release -- publish --Publish:Draft=<input>`.

The publish pipeline compiles every `Release_*` configuration of `Revit.Linter.slnx` with `TreatWarningsAsErrors=true`, verifies the localization artifacts, publishes the updater for `win-x64`, creates the MSI, and then requires a stable version (the `GitVersion` version equal to the MSI product version) and an existing installer. It writes the release notes from the changelog section, adds a link to that section at the release tag in GitHub Actions, and invokes `gh release create v<version> --verify-tag --notes-file <file> --title "Revit.Linter <version>" [--draft] <installer>`. The pipeline never passes `--prerelease`, so automation publishes stable releases only; a pre-release is published manually as described in [Release channels](#release-channels).

GitHub release creation is part of the pipeline: the workflow run is started manually, and the pipeline creates the release for the existing tag. After the pipeline completes, inspect the release build, the installer and the published artifacts.

## Related policies

- [Development policy](development.md) — the entry point to the contributor workflow.
- [Changelog policy](changelog.md) — the release section format and how it is composed.
- [Branching policy](branching.md) — release and hotfix branches and the tag on the merge commit.
- [Testing policy](testing.md) — the Revit test matrix that must run before the tag.
- [Build and toolchain policy](build.md) — build units, warnings and toolchain rules.

The rest of the contributor workflow is defined by the [development policy](development.md).
