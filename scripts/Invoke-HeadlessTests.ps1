<#
.SYNOPSIS
Runs the headless test executables of one build configuration, several at a time, and fails on a run
without tests.

.DESCRIPTION
Enumerates the projects ending in `.Tests.csproj`, selects the executable built for the given
configuration, runs the executables in parallel with a bounded throttle and writes a TRX report per
project. The script fails when a project has no executable, when an executable exits with a non-zero
code, or when a run discovers no tests: a broken discovery must not produce a green build.

Each test project runs as its own process over its own temporary data, so unrelated projects may run
at the same time; `-ThrottleLimit 1` restores a strictly sequential run.

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

.PARAMETER ThrottleLimit
Maximum number of test executables running at the same time. Defaults to the processor count, capped
at 8 to keep the memory of the copied test dependencies bounded.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Configuration,

    [string]$TestsDirectory = 'tests',

    [string]$ResultsDirectory,

    [string]$RepositoryRoot = (Get-Location).Path,

    [ValidateRange(1, 64)]
    [int]$ThrottleLimit = [Math]::Min([Environment]::ProcessorCount, 8)
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
if (-not (Test-Path -LiteralPath $ResultsDirectory -PathType Container)) {
    New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null
}

$configurationPattern = [regex]::Escape($Configuration)

# Build output under `bin` and `obj` holds thousands of files and never a project, so it is pruned
# while the test projects are discovered; a recursive `Get-ChildItem` over the whole tree took about
# 7 seconds per run on a developer machine.
function Get-TestProjectPaths {
    param([string]$Directory)

    $paths = [Collections.Generic.List[string]]::new()
    $directories = [Collections.Generic.Stack[string]]::new()
    $directories.Push($Directory)
    while ($directories.Count -gt 0) {
        $current = $directories.Pop()
        foreach ($path in [System.IO.Directory]::EnumerateFiles($current, '*.Tests.csproj', [System.IO.SearchOption]::TopDirectoryOnly)) {
            $paths.Add($path)
        }
        foreach ($child in [System.IO.Directory]::EnumerateDirectories($current)) {
            $name = [System.IO.Path]::GetFileName($child)
            if ($name -eq 'bin' -or $name -eq 'obj') { continue }
            $directories.Push($child)
        }
    }
    return $paths
}

$testProjects = @(Get-TestProjectPaths -Directory $testsPath |
    ForEach-Object { Get-Item -LiteralPath $_ } |
    Sort-Object -Property FullName)
if ($testProjects.Count -eq 0) {
    throw "No headless test projects were found under $TestsDirectory."
}

# Resolve every executable before the first run, so a missing build fails the whole script instead of
# leaving a half-run suite behind.
$jobs = [Collections.Generic.List[object]]::new()
foreach ($project in $testProjects) {
    $binDirectory = Join-Path $project.DirectoryName 'bin'
    if (-not (Test-Path -LiteralPath $binDirectory -PathType Container)) {
        throw "The build output of $($project.FullName) was not found. Build configuration $Configuration first."
    }

    # A solution build writes bin/x64/<configuration>, a direct `dotnet build` writes
    # bin/<configuration>; checking those two roots avoids walking every configuration under bin.
    $executables = @()
    foreach ($candidateRoot in @(
            (Join-Path $binDirectory (Join-Path 'x64' $Configuration)),
            (Join-Path $binDirectory $Configuration))) {
        if (-not (Test-Path -LiteralPath $candidateRoot -PathType Container)) { continue }
        $found = @([System.IO.Directory]::EnumerateFiles(
                $candidateRoot, "$($project.BaseName).exe", [System.IO.SearchOption]::AllDirectories))
        if ($found.Count -gt 0) {
            $executables = $found
            break
        }
    }
    if ($executables.Count -eq 0) {
        $executables = @(Get-ChildItem -LiteralPath $binDirectory -Recurse -File -Filter "$($project.BaseName).exe" |
            Where-Object { $_.FullName -match "[\\/]$configurationPattern[\\/]" } |
            ForEach-Object { $_.FullName })
    }
    if ($executables.Count -ne 1) {
        throw "Expected exactly one test executable for $($project.FullName) and configuration $Configuration; found $($executables.Count)."
    }

    $jobs.Add([pscustomobject]@{
            Project    = $project.BaseName
            Executable = $executables[0]
            TrxPath    = Join-Path $ResultsDirectory "$($project.BaseName).trx"
            Process    = $null
            Timer      = $null
            Duration   = [TimeSpan]::Zero
            ExitCode   = $null
            Output     = $null
            Error      = $null
            StdOut     = $null
            StdErr     = $null
        })
}

$queue = [Collections.Generic.Queue[object]]::new()
foreach ($job in $jobs) { $queue.Enqueue($job) }
$running = [Collections.Generic.List[object]]::new()
$wall = [Diagnostics.Stopwatch]::StartNew()

function Start-TestProcess {
    param($job)
    if (Test-Path -LiteralPath $job.TrxPath) { Remove-Item -LiteralPath $job.TrxPath -Force }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $job.Executable
    $startInfo.Arguments = ('-noColor -result-trx "{0}"' -f $job.TrxPath)
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    $job.Timer = [Diagnostics.Stopwatch]::StartNew()
    $job.Process = [System.Diagnostics.Process]::Start($startInfo)
    # Both streams have to be drained while the executable runs, otherwise a full pipe would stall it.
    $job.Output = $job.Process.StandardOutput.ReadToEndAsync()
    $job.Error = $job.Process.StandardError.ReadToEndAsync()
}

while ($queue.Count -gt 0 -or $running.Count -gt 0) {
    while ($queue.Count -gt 0 -and $running.Count -lt $ThrottleLimit) {
        $job = $queue.Dequeue()
        Start-TestProcess $job
        $running.Add($job)
    }

    Start-Sleep -Milliseconds 50
    foreach ($job in @($running)) {
        if (-not $job.Process.HasExited) { continue }
        $job.Timer.Stop()
        $job.Duration = $job.Timer.Elapsed
        $job.ExitCode = $job.Process.ExitCode
        # Both streams are drained while the executable runs; the reads complete once it exits.
        $job.StdOut = $job.Output.GetAwaiter().GetResult()
        $job.StdErr = $job.Error.GetAwaiter().GetResult()
        $job.Process.Dispose()
        $running.Remove($job) | Out-Null
    }
}
$wall.Stop()

# The console output of every project is buffered so parallel runs stay readable: each project writes
# to its own streams, and the text is replayed in the deterministic project order.
foreach ($job in $jobs) {
    if ($null -eq $job.Process) { continue }
    if ($job.StdOut) { Write-Host $job.StdOut }
    if ($job.StdErr) { Write-Host $job.StdErr }
}

$rows = [Collections.Generic.List[object]]::new()
$failures = [Collections.Generic.List[string]]::new()

foreach ($job in $jobs) {
    if ($job.ExitCode -ne 0) {
        $failures.Add("$($job.Project) exited with code $($job.ExitCode).")
        $rows.Add([pscustomobject]@{
                Project  = $job.Project
                Total    = '?'
                Failed   = '?'
                Duration = $job.Duration
            })
        continue
    }

    if (-not (Test-Path -LiteralPath $job.TrxPath -PathType Leaf)) {
        $failures.Add("$($job.Project) wrote no TRX report, so the test result cannot be verified.")
        continue
    }

    [xml]$report = Get-Content -LiteralPath $job.TrxPath
    $counters = $report.TestRun.ResultSummary.Counters
    $total = [int]$counters.total
    if ($total -eq 0) {
        $failures.Add("$($job.Project) discovered no tests for configuration $Configuration.")
        $rows.Add([pscustomobject]@{
                Project  = $job.Project
                Total    = 0
                Failed   = 0
                Duration = $job.Duration
            })
        continue
    }

    $rows.Add([pscustomobject]@{
            Project  = $job.Project
            Total    = $total
            Failed   = [int]$counters.failed
            Duration = $job.Duration
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
    $summary.Add("| $($row.Project) | $($row.Total) | $($row.Failed) | $([math]::Round($row.Duration.TotalSeconds, 1)) s |")
}
if ($rows.Count -eq 0) {
    $summary.Add('| (no project ran) | - | - | - |')
}
$summary.Add('')
$summary.Add("Wall clock: $([math]::Round($wall.Elapsed.TotalSeconds, 1)) s, throttle limit: $ThrottleLimit.")
$summary | ForEach-Object { Write-Host $_ }
if ($env:GITHUB_STEP_SUMMARY) {
    $summary | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}

if ($failures.Count -gt 0) {
    throw "Headless tests failed for configuration ${Configuration}:`n$($failures -join "`n")"
}

Write-Host "Headless tests passed for configuration $Configuration ($($rows.Count) project(s))."
