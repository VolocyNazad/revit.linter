# Contributing

Before making a change, read the [development policy](docs/policies/development.md) and open the
[repository policy index](docs/policies/README.md) for the one policy that covers the area you touch.
The [repository guide](docs/repository.md) is a reference: read the section for that area, not the whole file.

Branches follow the [branching policy](docs/policies/branching.md): `main` holds
the released state, `dev` integrates the next release, and every change arrives
through a short-lived `feature/`, `fix/`, `chore/`, `release/` or `hotfix/` branch and a
pull request. The [pull request policy](docs/policies/pull-requests.md) defines what a pull
request must contain and which of the [templates](docs/policies/pull-requests.md#template)
matches the branch.

Commit messages follow the [commit message policy](docs/policies/commits.md).

Tests follow the [testing policy](docs/policies/testing.md): `*.Tests` run in CI on
every supported Revit version, `*.RevitTests` run outside CI on a machine with Revit
installed.

Keep changes focused. A change carries its changelog entry as one fragment in `changelog.d/`, in
the same branch as the change; regular development leaves `## [Unreleased]` in
[CHANGELOG.md](CHANGELOG.md) empty and the release section is composed when a release is prepared.
See the [changelog policy](docs/policies/changelog.md).
Before submitting a pull request, check the items that apply to the change:

- Assess whether automated tests or a manual Revit check are needed; add or update
  tests when appropriate. Follow the test approval rule in the development policy.
  Built-in diagnostics follow [docs/diagnostics/adding.md](docs/diagnostics/adding.md).
- Build the affected projects and configurations. Resolve new compiler and
  analyzer warnings in touched code.
- Confirm relevant CI checks pass before merging.
- Review compatibility with saved settings, configuration files and upgrade paths
  when those are affected.
- Update user documentation and localized strings when user-visible behavior or
  text changes.
- Review logging for significant operations, refusals and failures when behavior
  changes; follow the [logging policy](docs/policies/logging.md).
- Add the changelog fragment and update the repository documentation when required by the
  development policy.
- Run the repository policy validator after changing repository-level files,
  documentation or the solution:

```powershell
./scripts/Validate-Repository.ps1
```

## Before publishing a release

The [release policy](docs/policies/releases.md) defines the release channels, tags and metadata.

- Verify the release tag and version against the release commit.
- Compose the new `MAJOR.MINOR.PATCH` section from the changes since the previous
  stable tag and leave `Unreleased` empty, as described in the
  [changelog policy](docs/policies/changelog.md).
- Confirm CI succeeds and inspect the release build, installer and published
  artifacts when the release pipeline completes.
