# Release policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: preparing a release tag, version or MSI.

A release turns a tagged commit into a GitHub release with installers. The workflow is
`.github/workflows/publish-release.yml` and the publish modules are under `build/`; the pipeline
consumes the tag and the release section of `CHANGELOG.md`. The [changelog policy](changelog.md) owns
that section's format, the [branching policy](branching.md) owns release and hotfix branches, and the
[testing policy](testing.md) owns what must be green before the tag.

## Release channels

- A **stable release** is a `vMAJOR.MINOR.PATCH` tag and the only channel the updater offers. Publish
  its GitHub Release without the **Set as a pre-release** flag, so `/releases/latest` returns it.
- A **pre-release** is a `vMAJOR.MINOR.PATCH-<channel>.<number>` tag, for example `-beta.1` or
  `-rc.1`. Use it to reach testers before the stable release with a risky migration, a new installer
  or a large diagnostic change. Publish it with the **Set as a pre-release** flag. The updater never
  announces pre-releases, GitHub does not treat them as `latest`, and testers download them from the
  Releases page.

The updater discovers releases through `/releases/latest`, which returns only the latest published
release that is neither a draft nor a pre-release; the client rejects draft and pre-release payloads.

Current automation publishes stable releases only: the release pipeline requires `GitVersion` to
produce a stable version and invokes `gh release create` without `--prerelease`. Publish a pre-release
manually with `gh release create vX.Y.Z-beta.N --prerelease ...`, or extend the pipeline first.

## Versioning

- `GitVersion` runs in Continuous Delivery mode with patch increments. The tag is the version source:
  a release commit carries exactly one stable `vMAJOR.MINOR.PATCH` tag.
- The build uses the tagged version without the `v` prefix for the binaries and the MSI. The MSI
  product version is the numeric `MAJOR.MINOR.PATCH`; a suffix-bearing version is reduced to those
  three numbers, while the add-in and updater assemblies keep the full `GitVersion` semantic version.
- The repository publishes a GitHub release with installers instead of a NuGet package.
- The tagged commit already contains the release section in `CHANGELOG.md` with an empty
  `## [Unreleased]` above it and no pending fragment in `changelog.d/`; the pipeline publishes that
  section as the GitHub release description.
  Do not compose or edit that section outside the [changelog policy](changelog.md).

## Preparing a release

Before creating the tag:

- Confirm the full Revit test matrix is executed and recorded in the release pull request, as the
  [testing policy](testing.md) requires.
- Compose and finalize the release section with `./scripts/Update-Changelog.ps1` and confirm that
  `changelog.d/` holds no fragment, as the [changelog policy](changelog.md) requires.
- Confirm the build is green for every supported `Release_*` configuration, the working tree is clean
  and the release changelog section is committed.
- Confirm `repository-policy` passed on the release pull request.
- Verify the release tag and version against the release commit.
- Keep every release configuration warning-free; the pull request matrix and the publish both treat
  warnings as errors.
- Confirm CI succeeds before the release pipeline runs.

Prepare the release branch as the [branching policy](branching.md) requires; its branch and merge
steps are not repeated here.

## Release metadata rules

- Never mark a stable version as a pre-release. That hides it from `/releases/latest` and the updater.
- Never give a pre-release a stable version number. Use `vX.Y.Z-beta.N`; `vX.Y.Z` is reserved for the
  stable release that follows it.
- Never delete, move or force-push a pushed release tag. A pushed tag is immutable; publish the next
  version instead. If a mistake is found before the tag is pushed and no release was published, fix
  the commit, move the local tag and push it once.
- If a stable release was published with the pre-release flag by mistake, correct the GitHub release
  metadata by unchecking the flag; never move or delete the tag. Keep its assets: the release page
  and changelog link to them.
- Only the stable release has a `CHANGELOG.md` version section. Pre-release notes may summarize the
  changes for testers, but they do not create a permanent changelog section.

## Release artifacts and MSI

- Publish the updater and the Windows App SDK notification runtime for `win-x64` as a self-contained
  unpackaged application before MSI creation from the explicit build-module result, not from
  configuration-specific paths under `bin`.
- Verify the ILRepack output and the localization satellites before creating the MSI.
- Produce one per-user MSI containing every supported Revit build and the shared updater. It installs
  under `%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, creates version-specific manifests under
  `%APPDATA%\Autodesk\Revit\Addins\<year>\`, and registers the updater under the current user's `Run`
  key.
- Keep one stable `UpgradeCode`; the product code changes deterministically with the stable product
  version. The first unified installation detects and removes older per-Revit-version products by
  their legacy upgrade codes.
- Expose one required `Core` feature and one optional feature per supported Revit year. Preselect
  installed years through the `ProductName` lookup under
  `HKLM\SOFTWARE\Autodesk\Revit\Autodesk Revit <year>\Components`, and let the feature-tree UI
  override that selection. Synchronize the manifests with the requested feature states on install,
  repair and modify.
- Before changing any installed file, stop the short-lived updater and refuse to continue while a
  Revit process is running.
- Publish the installer asset as `RevitLinter-<version>.msi`, the name defined by
  `ProductIdentity.GetMsiAssetName`. The updater accepts a release installer only when its GitHub
  asset name and HTTPS URL exactly match the stable release version.

## Publish automation

The workflow runs on `workflow_dispatch` with a boolean `draft` input (default `false`); a tag push
alone does not trigger it.

The workflow:

- checks out with full history without persisting credentials, installs the SDK from `global.json`, the
  cached NuGet packages and WiX 6.0.2;
- runs `./scripts/Verify-Release.ps1 -FromHeadTag -AncestorOf main`: `HEAD` must carry exactly one
  stable tag matching `vMAJOR.MINOR.PATCH`, `CHANGELOG.md` must have a `## [Unreleased]` section with
  no entries below it and exactly one `## [VERSION] - YYYY-MM-DD` section for the tag directly below
  it, the release history window must hold - one major line in the changelog, older releases in its
  archive - and `HEAD` must be an ancestor of `origin/main`;
- runs `dotnet run --project build/Revit.Linter.Build.csproj -c Release -- publish --Publish:Draft=<input>`;
- has a 120-minute job timeout.

The `repository-policy` workflow runs the structural part of the same script on every push and pull
request, so a malformed changelog is reported before the immutable tag exists.

The publish pipeline compiles every `Release_*` configuration of `Revit.Linter.slnx` with
`TreatWarningsAsErrors=true`, verifies the localization artifacts, publishes the updater for `win-x64`,
creates the MSI, and then requires a stable version (the `GitVersion` version equal to the MSI product
version) and an existing installer. It writes the release notes from the changelog section, adds a
link to that section at the release tag in GitHub Actions, and invokes
`gh release create v<version> --verify-tag --notes-file <file> --title "Revit.Linter <version>" [--draft] <installer>`.
The pipeline never passes `--prerelease`; a pre-release is published manually as described in
[Release channels](#release-channels).

After the pipeline, inspect the release build, the installer and the published artifacts.

## Related policies

- [Development policy](development.md) — the contributor workflow entry point.
- [Changelog policy](changelog.md) — the release section format.
- [Branching policy](branching.md) — branches and the merge-commit tag.
- [Testing policy](testing.md) — the pre-tag test matrix.
- [Build and toolchain policy](build.md) — build units and warnings.

The rest of the contributor workflow is defined by the [development policy](development.md).
