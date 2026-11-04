<#
.SYNOPSIS
Runs the headless test executables of one build configuration and fails on a run without tests.

.DESCRIPTION
Enumerates the projects ending in `.Tests.csproj`, selects the executable built for the given
configuration, runs it and writes a TRX report per project. The script fails when a project has no
executable, when an executable exits with a non-zero code, or when a run discovers no tests: a
broken discovery must not produce a green build.

The same script runs in CI and locally:

    ./scripts/Invoke-HeadlessTests.ps1 -Configuration Release_2027.3.0

    ./scripts/Invoke-HeadlessTests.ps1 -Configuration Release -TestsDirectory updater/tests

.PARAMETER Configuration
The solution configuration the tests were built with, for example `Release_2027.3.0`.

.PARAMETER TestsDirectory
Repository-relative directory that contains the test projects. Defaults to `tests`.

.PARAMETER ResultsDirectory
Directory for the TRX reports. Defaults to `TestResults/<Configuration>`.

.PARAMETER RepositoryRoot
Repository root. Defaults to the current directory.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Configuration,

    [string]$TestsDirectory = 'tests',

    [string]$ResultsDirectory,

    [string]$RepositoryRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$testsPath = Join-Path $root $TestsDirectory
if (-not (Test-Path -LiteralPath $testsPath -PathType Container)) {
    throw "The test directory does not exist: $TestsDirectory."
}

if (-not $ResultsDirectory) {
    $ResultsDirectory = Join-Path (Join-Path $root 'TestResults') $Configuration
}
New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null

$configurationPattern = [regex]::Escape($Configuration)
$testProjects = @(Get-ChildItem -LiteralPath $testsPath -Recurse -File -Filter '*.csproj' |
    Where-Object { $_.Name.EndsWith('.Tests.csproj', [StringComparison]::Ordinal) } |
    Sort-Object -Property FullName)
if ($testProjects.Count -eq 0) {
    throw "No headless test projects were found under $TestsDirectory."
}

$rows = [Collections.Generic.List[object]]::new()
$failures = [Collections.Generic.List[string]]::new()

foreach ($project in $testProjects) {
    $binDirectory = Join-Path $project.DirectoryName 'bin'
    if (-not (Test-Path -LiteralPath $binDirectory -PathType Container)) {
        throw "The build output of $($project.FullName) was not found. Build configuration $Configuration first."
    }

    $executables = @(Get-ChildItem -LiteralPath $binDirectory -Recurse -File -Filter "$($project.BaseName).exe" |
        Where-Object { $_.FullName -match "[\\/]$configurationPattern[\\/]" })
    if ($executables.Count -gt 1) {
        # A direct `dotnet build` of a project writes bin/<configuration>; the solution builds the x64
        # platform under bin/x64. Prefer the solution output so a local run matches the CI build.
        $executables = @($executables | Where-Object { $_.FullName -match '[\\/]x64[\\/]' })
    }
    if ($executables.Count -ne 1) {
        throw "Expected exactly one test executable for $($project.FullName) and configuration $Configuration; found $($executables.Count)."
    }

    $trxPath = Join-Path $ResultsDirectory "$($project.BaseName).trx"
    if (Test-Path -LiteralPath $trxPath) { Remove-Item -LiteralPath $trxPath -Force }

    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    & $executables[0].FullName -noColor -result-trx $trxPath
    $exitCode = $LASTEXITCODE
    $stopwatch.Stop()

    if ($exitCode -ne 0) {
        $failures.Add("$($project.BaseName) exited with code $exitCode.")
        $rows.Add([pscustomobject]@{
            Project = $project.BaseName
            Total = '?'
            Failed = '?'
            Duration = $stopwatch.Elapsed
        })
        continue
    }

    if (-not (Test-Path -LiteralPath $trxPath -PathType Leaf)) {
        $failures.Add("$($project.BaseName) wrote no TRX report, so the test result cannot be verified.")
        continue
    }

    [xml]$report = Get-Content -LiteralPath $trxPath
    $counters = $report.TestRun.ResultSummary.Counters
    $total = [int]$counters.total
    if ($total -eq 0) {
        $failures.Add("$($project.BaseName) discovered no tests for configuration $Configuration.")
        $rows.Add([pscustomobject]@{
            Project = $project.BaseName
            Total = 0
            Failed = 0
            Duration = $stopwatch.Elapsed
        })
        continue
    }

    $rows.Add([pscustomobject]@{
        Project = $project.BaseName
        Total = $total
        Failed = [int]$counters.failed
        Duration = $stopwatch.Elapsed
    })
}

$summary = [Collections.Generic.List[string]]::new()
$summary.Add('## Headless tests')
$summary.Add('')
$summary.Add("Configuration: ``$Configuration``")
$summary.Add('')
$summary.Add('| Project | Total | Failed | Duration |')
$summary.Add('| --- | --- | --- | --- |')
foreach ($row in $rows) {
    $summary.Add("| $($row.Project) | $($row.Total) | $($row.Failed) | $([int]$row.Duration.TotalSeconds) s |")
}
if ($rows.Count -eq 0) {
    $summary.Add('| (no project ran) | - | - | - |')
}
$summary | ForEach-Object { Write-Host $_ }
if ($env:GITHUB_STEP_SUMMARY) {
    $summary | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}

if ($failures.Count -gt 0) {
    throw "Headless tests failed for configuration ${Configuration}:`n$($failures -join "`n")"
}

Write-Host "Headless tests passed for configuration $Configuration ($($rows.Count) project(s))."
