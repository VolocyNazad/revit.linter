# Revit.Linter

[![Revit 2021, 2023, 2025](https://img.shields.io/badge/Revit-2021%20%7C%202023%20%7C%202025-green.svg)](https://www.autodesk.com/products/revit/overview)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/VolocyNazad/revit.linter)](https://github.com/VolocyNazad/revit.linter/releases)

Revit.Linter is an extension for Autodesk Revit that helps find issues in projects and families before they affect documentation output or collaborative work.

## Features

- built-in diagnostics for documents, families, and elements;
- user diagnostics based on YAML configurations;
- collision detection and project parameter checks;
- filtering, sorting, and step-by-step navigation through the findings;
- navigation from a message to the problem element and reversible element highlighting;
- configurable activity and severity level for diagnostics;
- ignoring a finding for one element or for a whole document, with the ignore list kept inside the model;
- fixes applied through Revit-safe transactions, with the results of the session in the fix list panel;
- report export as a self-contained HTML page or as CSV, JSON and YAML data;
- a first-run welcome wizard and an optional practical tour inside Revit;
- update checks through the standalone updater, announced with a Windows notification.

## Supported versions

Ready-made builds are released for Revit 2021, 2023, and 2025.

## Installation

1. Open the [Releases](https://github.com/VolocyNazad/revit.linter/releases) page.
2. Download the installer (one MSI carries every supported Revit version and preselects the ones installed on the computer).
3. Close Revit and run the installer.
4. After installation, open Revit — the extension's commands will appear on the **Diagnostics** tab.

## Usage

On the **Diagnostics** tab you can open the diagnostics, results, fixes and ignore-list panels. The configuration folder is opened from the Diagnostics pane toolbar.

Diagnostics are configured with YAML files, separately for each version of Revit. See the [user documentation](wiki/Home.md) for the interface, built-in diagnostics, formulas, and configuration format. The `wiki/` folder contains English and Russian documentation and can also be opened directly as an Obsidian vault.

## License

The project is distributed under the [Apache License 2.0](LICENSE).

## Development documentation

- [Development policy](docs/policies/development.md)
- [Repository guide and technology stack](docs/repository.md)

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.
