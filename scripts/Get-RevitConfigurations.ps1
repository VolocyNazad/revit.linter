<#
.SYNOPSIS
Prints the supported Revit configurations declared by the root Directory.Build.props.

.DESCRIPTION
`Directory.Build.props` declares the supported Revit version set. The CI workflow resolves its build
matrix from this file, so the matrix and the policy list follow one source; the solution
configurations and the testing-policy list must match it and are checked by
`scripts/Validate-Repository.ps1`.

.PARAMETER RepositoryRoot
Repository root. Defaults to the current directory.

.PARAMETER Format
`Json` prints a JSON array for a GitHub Actions matrix; `Text` prints one configuration per line.
#>
param(
    [string]$RepositoryRoot = (Get-Location).Path,

    [ValidateSet('Json', 'Text')]
    [string]$Format = 'Json'
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$propsPath = Join-Path $root 'Directory.Build.props'
if (-not (Test-Path -LiteralPath $propsPath -PathType Leaf)) {
    throw 'Directory.Build.props was not found.'
}

[xml]$props = Get-Content -LiteralPath $propsPath
$declared = @($props.Project.PropertyGroup | ForEach-Object { $_.Configurations } | Where-Object { $_ }) -join ';'
$configurations = @($declared -split '[;,\s]+' |
    Where-Object { $_ -match '^Release_\d+\.\d+\.\d+$' } |
    Select-Object -Unique |
    Sort-Object)

if ($configurations.Count -eq 0) {
    throw "Directory.Build.props declares no Release_<year>.<major>.<minor> configuration."
}

if ($Format -eq 'Text') {
    $configurations
} else {
    '[' + (($configurations | ForEach-Object { '"' + $_ + '"' }) -join ',') + ']'
}
