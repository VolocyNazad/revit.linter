param([string]$RepositoryRoot = (Get-Location).Path)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing required file: $RelativePath" }
    if ((Get-Item -LiteralPath $path).Length -eq 0) { throw "Required file is empty: $RelativePath" }
}
function Require-One([string]$Label, [string[]]$Names) {
    $found = @(Get-ChildItem -LiteralPath $root -File | Where-Object { $_.Name -in $Names })
    if ($found.Count -ne 1) { throw "Expected exactly one $Label file; found: $($found.Name -join ', ')" }
}
function Require-Link([string]$Document, [string]$Target) {
    $text = Get-Content -LiteralPath (Join-Path $root $Document) -Raw
    if ($text -notmatch [regex]::Escape("($Target)")) { throw "$Document must link to $Target" }
}

@('README.md', 'AGENTS.md', 'CHANGELOG.md', 'CONTRIBUTING.md', 'global.json',
  'docs/policies/development.md', 'docs/repository.md',
  'scripts/Validate-Repository.ps1') | ForEach-Object { Require-File $_ }
Require-One 'NuGet configuration' @('NuGet.config', 'nuget.config')
Require-One 'license' @('LICENSE', 'LICENSE.md', 'LICENSE.txt')
Require-One 'root solution' @((Get-ChildItem -LiteralPath $root -File |
    Where-Object Extension -in '.sln', '.slnx' | ForEach-Object Name))

Require-Link 'AGENTS.md' 'docs/policies/development.md'
Require-Link 'AGENTS.md' 'docs/repository.md'
Require-Link 'README.md' 'CONTRIBUTING.md'
Require-Link 'README.md' 'docs/policies/development.md'
Require-Link 'README.md' 'docs/repository.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/development.md'
Require-Link 'CONTRIBUTING.md' 'docs/repository.md'
Require-Link 'CONTRIBUTING.md' 'CHANGELOG.md'
Require-Link 'docs/policies/development.md' '../repository.md'
Require-Link 'docs/repository.md' 'policies/development.md'

$managed = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$rootNames = @('.gitignore', '.gitattributes', '.editorconfig', '.globalconfig', 'global.json',
    'nuget.config', 'gitversion.yml', 'gitversion.yaml', 'license', 'license.md', 'license.txt')
Get-ChildItem -LiteralPath $root -File | Where-Object {
    $_.Name.ToLowerInvariant() -in $rootNames -or
    $_.Extension.ToLowerInvariant() -in @('.md', '.props', '.targets', '.ps1', '.cmd', '.bat', '.sh', '.ruleset', '.runsettings')
} | ForEach-Object { [void]$managed.Add($_.Name) }
foreach ($folder in @('docs', '.github', 'scripts')) {
    $folderPath = Join-Path $root $folder
    if (Test-Path -LiteralPath $folderPath -PathType Container) {
        Get-ChildItem -LiteralPath $folderPath -File -Recurse | Where-Object {
            $_.FullName -notmatch '[\\/](\.git|bin|obj|\.obsidian)[\\/]'
        } | ForEach-Object {
            [void]$managed.Add($_.FullName.Substring($root.Length + 1).Replace('\', '/'))
        }
    }
}

$solution = @(Get-ChildItem -LiteralPath $root -File | Where-Object Extension -in '.sln', '.slnx')[0]
if ($solution.Extension -eq '.slnx') {
    [xml]$xml = Get-Content -LiteralPath $solution.FullName -Raw
    $listed = @($xml.SelectNodes('//File') | ForEach-Object { $_.Path.Replace('\', '/') })
} else {
    $text = Get-Content -LiteralPath $solution.FullName -Raw
    $listed = @([regex]::Matches($text, '(?ms)ProjectSection\(SolutionItems\).*?\n(.*?)\s*EndProjectSection') |
        ForEach-Object { [regex]::Matches($_.Groups[1].Value, '(?m)^\s*(.*?)\s*=') } |
        ForEach-Object { $_.Groups[1].Value.Trim().Replace('\', '/') })
}
$duplicates = @($listed | Group-Object { $_.ToLowerInvariant() } | Where-Object Count -gt 1)
if ($duplicates) { throw "Duplicate solution items: $($duplicates.Name -join ', ')" }
foreach ($item in $listed) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $item) -PathType Leaf)) { throw "Broken solution item: $item" }
}
$missingItems = @($managed | Where-Object { $_ -notin $listed } | Sort-Object)
if ($missingItems) { throw "Files missing from Solution Items: $($missingItems -join ', ')" }

Write-Host "Repository policy validation passed: $($solution.Name); $($listed.Count) solution items."
