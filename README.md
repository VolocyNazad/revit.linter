# Revit.Linter

[![Revit 2021, 2023, 2025](https://img.shields.io/badge/Revit-2021%20%7C%202023%20%7C%202025-green.svg)](https://www.autodesk.com/products/revit/overview)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/VolocyNazad/revit.linter)](https://github.com/VolocyNazad/revit.linter/releases)

Revit.Linter is an extension for Autodesk Revit that helps find issues in projects and families before they affect documentation output or collaborative work.

## Features

- built-in diagnostics for documents, families, and elements;
- user diagnostics based on YAML configurations;
- collision detection and project parameter checks;
- filtering and sorting of results;
- navigation from a message to the problem element;
- configurable activity and severity level for diagnostics;
- saved history of diagnostics and fixes.

## Supported versions

Ready-made builds are released for Revit 2021, 2023, and 2025.

## Installation

1. Open the [Releases](https://github.com/VolocyNazad/revit.linter/releases) page.
2. Download the installer for your version of Revit.
3. Close Revit and run the installer.
4. After installation, open Revit — the extension's commands will appear on the **Volocy** tab.

## Usage

On the **Volocy** tab you can open the diagnostics, results, and fixes panels, as well as navigate to the configuration folder.

Diagnostics are configured with YAML files, separately for each version of Revit. A detailed description of the interface, built-in diagnostics, formulas, and configuration format is being prepared in the [documentation](docs/documentation.md).

## License

The project is distributed under the [Apache License 2.0](LICENSE).

## Development documentation

- [Development policy](docs/policies/development.md)
- [Repository guide and technology stack](docs/repository.md)

## Contributing
 [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.
