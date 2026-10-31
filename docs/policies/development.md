# Development policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The stack described in the [repository guide](../repository.md) is used by default and takes priority over
whatever the agent might choose on its own. Prefer whatever is already used
in the affected part of the repository over an alternative. If an alternative
seems better — don't substitute it silently: ask the user,
explain the reason, and wait for confirmation.

If a change affects the folder structure or the technology stack (a new/removed project, a new dependency, a version bump worth recording, a new convention) — update the relevant policy and [repository guide](../repository.md) as part of the same change, not as a separate follow-up later.

Before changing existing tests or writing new ones, ask the user first — clarify what and how it should be covered (or that the change is trivial enough that no clarification is needed), rather than deciding this on your own.

Do not `git push` — commit locally and leave the push to the user, unless they explicitly ask you to push.

## Where the rules live

The [policy index](README.md) is the full list. The policies a change usually touches:

| Topic | Policy |
| --- | --- |
| Branch, base branch, merge strategy | [Branching policy](branching.md) |
| Commit message | [Commit message policy](commits.md) |
| `CHANGELOG.md` | [Changelog policy](changelog.md) |
| Pull request, review, required checks | [Pull request policy](pull-requests.md) |
| Tests and Revit version matrix | [Testing policy](testing.md) |
| Benchmarks | [Benchmark policy](benchmarks.md) |
| Release, tags, versioning, MSI | [Release policy](releases.md) |
| Logging and failure reporting | [Logging policy](logging.md) |
| Localized resources and cultures | [Localization policy](localization.md) |
| WPF views, view models, design-time data | [Presentation policy](presentation.md) |
| User-visible migrations | [Migration policy](migrations.md) |
| .NET SDK, build units, packages, warnings | [Build and toolchain policy](build.md) |
| Solution items, validator, formatting | [Repository maintenance policy](repository-maintenance.md) |
| Contributor documentation and XML docs | [Documentation policy](documentation.md) |
| Published user documentation | [Wiki policy](wiki.md) |

Module boundaries, abstractions and dependency injection follow the stable rules in
[architecture decisions](../architecture.md). When a change introduces or revises a fundamental
architectural decision, update that document as part of the same change.

## Code basics

Do not invent abbreviations in identifiers, file names or documentation. Use complete words such as `ViewModel` and `Configuration`; established terms such as `API`, `WPF`, `XML`, `Guid`, `Id` and `Uri` remain valid.

Use `global using` only when most files in that project need the namespace. Keep specialized namespaces local, and do not repeat namespaces already supplied by `ImplicitUsings`. Apply this rule to new and touched code; clean up existing files only when doing so stays within the task scope.

Keep [CHANGELOG.md](../../CHANGELOG.md) in the state the [changelog policy](changelog.md) requires: regular development leaves `## [Unreleased]` empty, and the release section is composed when a release is prepared.
