## Policy

Стек, описанный ниже, используется по умолчанию и имеет приоритет над
тем, что агент мог бы выбрать сам. Предпочитайте то, что уже используется
в затрагиваемой части репозитория, а не альтернативу — даже если она
кажется более удачной. Если отклонение от стека действительно необходимо,
не делайте это молча: явно скажите об этом пользователю, объясните
причину и дождитесь подтверждения.

## О проекте

Revit.Linter — расширение для Autodesk Revit, которое позволяет пользователю следить за чистотой проектов и семейств.

## Решение и структура

- `Revit.Linter.slnx` — solution
- `src/` - projects
- `tests/` - tests
- `docs/` - documentation
- `installer/` -msi installer
- `build/` - solution building, compilation, package
- `output/` - artifacts after building
- `benchmark/` - benchmarking
- `sandbox/` - отдельное solution (`Revit.Linter.Sandbox.slnx`) для
  локальных экспериментов, не часть основной сборки/тестов
- `wiki/` - Obsidian-вики с документацией (`Home.md`, `Setup.md`)

`src/` содержит десятки небольших проектов по одной ответственности на
проект (диагностики, презентеры, менеджеры состояния и т.д.) — большинство
названо `Revit.Linter.<Область>`. `Toolkit.Revit.Extensions` — расширения
для Revit API, отдельные от `Revit.Linter.*`.

## Технологический стек

- `VolocyNazad.Revit.Sdk` (кастомный MSBuild SDK, исходники в отдельном
  репозитории `toolkit.revit.sdk`) + `Revit_All_Main_Versions_API_x64`
- WPF, CommunityToolkit.Mvvm, MaterialDesignThemes, Microsoft.Xaml.Behaviors.Wpf
- `VolocyNazad.Revit.Async`, `VolocyNazad.Revit.Context`,
  `VolocyNazad.Revit.Events`, `VolocyNazad.Revit.TransactionMemoryCache`,
  `VolocyNazad.MVVM.DependencyInjection`, `VolocyNazad.AssemblyResolver` —
  пакеты из семейства репозиториев `toolkit.revit.*`
- `Nice3point.TUnit.Revit` — тестирование внутри среды Revit
- Microsoft.Extensions.* (DependencyInjection, Logging, Localization,
  Options, Hosting)
- Microsoft.CodeAnalysis.CSharp (Roslyn — вероятно, для анализа кода/парсинга)
- YamlDotNet, StringToExpression, Humanizer.Core(.ru)
- Serilog + Serilog.Sinks.* (Console, Debug, File)
- ILRepack (объединение сборок при публикации)
- Тесты: **xunit.v3** + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk
- Central package management через `Directory.Packages.props`;
  AutoConstructor, PolySharp, SonarAnalyzer.CSharp подключены глобально
  через `GlobalPackageReference` на все проекты
