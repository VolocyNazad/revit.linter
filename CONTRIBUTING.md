# Contributing

Before making a change, read the [development policy](docs/policies/development.md)
and the [repository guide](docs/repository.md). They define the repository-specific
stack, testing expectations and maintenance rules.

Branches follow the [branching policy](docs/policies/branching.md): `main` holds
the released state, `dev` integrates the next release, and every change arrives
through a short-lived `feature/`, `fix/`, `release/` or `hotfix/` branch and a
pull request.

Commit messages follow the [commit message policy](docs/policies/commits.md).

Tests follow the [testing policy](docs/policies/testing.md): `*.Tests` run in CI on
every supported Revit version, `*.RevitTests` run outside CI on a machine with Revit
installed.

Keep changes focused. Regular development leaves `## [Unreleased]` in
[CHANGELOG.md](CHANGELOG.md) empty: the release section is composed from the changes
since the previous stable tag when a release is prepared. See the
[changelog policy](docs/policies/changelog.md).
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
  changes; follow the [logging policy](docs/policies/development.md#logging).
- Update the changelog and repository documentation when required by the
  development policy.
- Run the repository policy validator after changing repository-level files,
  documentation or the solution:

```powershell
./scripts/Validate-Repository.ps1
```

## Before publishing a release

- Verify the release tag and version against the release commit.
- Compose the new `MAJOR.MINOR.PATCH` section from the changes since the previous
  stable tag and leave `Unreleased` empty, as described in the
  [changelog policy](docs/policies/changelog.md).
- Confirm CI succeeds and inspect the release build, installer and published
  artifacts when the release pipeline completes.
