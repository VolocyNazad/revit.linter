# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- Use the Material Design light or dark palette for panel backgrounds instead of the Revit frame color.

- Preserve unknown report message placeholders verbatim instead of dropping their braces, and support `{{`/`}}` escapes for literal braces.

- Route report message element links through `Hyperlink.Command` with a typed id parameter instead of `Click` handlers parsing the `Tag` string, and fix the fix-report accent delegate to use `long` ids on Revit 2024+.

- Carry report message element links as typed `ElementId` values from parsing to the accent command instead of round-tripping through strings.

- Parse and cache each report message as one text-and-parts model, then render it in a shared typed view without positional multi-bindings.

- Fix element accent command guards to accept `long` ids on Revit 2024+ and make the fix-report item code immutable.

- Build each report message `FlowDocument` once and reuse it instead of recreating it on every binding access.

- Move report message link and text styling from view models to XAML: links use the theme-aware primary palette with a hover state instead of hardcoded blue, and both reports share the same message font.

- Handle nullable document titles, localization arguments, and the executing assembly directory explicitly.

- Build collision indexing and JSON report export for the .NET Framework configurations used by Revit 2021 and 2023.

### Added

- Build the supported Revit matrix and run headless tests on pushes and pull requests.

- Validate required repository files, navigation links, and Solution Items in CI.

### Changed

- Extract the duplicated report message template parser from the diagnostic and fix report presenters into the shared `Revit.Linter.ReportMessaging` project with headless tests.

- Standardize GitHub Actions workflow filenames and display names by responsibility.

- Set the Roslyn compatibility baseline to Microsoft.CodeAnalysis 4.14 for Visual Studio 2022 version 17.14.

- Configure xUnit v3 test execution through Microsoft.Testing.Platform and fail test runs when no tests are discovered.

- Establish a shared EditorConfig baseline and use the repository-policy validator as the single structural CI check.

- Complete solution items for repository documents, configuration, workflows and maintenance scripts; document the shared layout.

- Standardize local and CI SDK selection on stable .NET 10.0 through global.json, restrict roll-forward to that major/minor line, and configure setup-dotnet to read the file.

- Show documentation in Visual Studio Solution Explorer under a `docs` solution folder with matching subfolders.

- Move development policies and repository guidance from `AGENTS.md` to `docs/policies/development.md` and `docs/repository.md`; keep required reading links in `AGENTS.md` and add README navigation.
