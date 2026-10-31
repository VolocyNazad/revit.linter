param([string]$RepositoryRoot = (Get-Location).Path)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path

function Read-Utf8File([string]$Path) {
    return [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
}
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
    $text = Read-Utf8File (Join-Path $root $Document)
    if ($text -notmatch [regex]::Escape("($Target)")) { throw "$Document must link to $Target" }
}

# --- Required files --------------------------------------------------------------------------
$policies = @(
    'development', 'branching', 'commits', 'changelog', 'testing', 'benchmarks', 'wiki',
    'releases', 'logging', 'localization', 'presentation', 'migrations', 'build',
    'repository-maintenance', 'documentation', 'pull-requests')
@('README.md', 'AGENTS.md', 'CHANGELOG.md', 'CONTRIBUTING.md', 'global.json',
  'docs/policies/README.md', 'docs/repository.md', 'docs/architecture.md', 'docs/FEATURES.md',
  'docs/migrations/README.md', 'scripts/Validate-Repository.ps1') | ForEach-Object { Require-File $_ }
$policies | ForEach-Object { Require-File "docs/policies/$_.md" }
Require-One 'NuGet configuration' @('NuGet.config', 'nuget.config')
Require-One 'license' @('LICENSE', 'LICENSE.md', 'LICENSE.txt')
Require-One 'root solution' @((Get-ChildItem -LiteralPath $root -File |
    Where-Object Extension -in '.sln', '.slnx' | ForEach-Object Name))

# --- Navigation links ------------------------------------------------------------------------
# The index is the single list of policies; the guides and the entry points link to it.
Require-Link 'AGENTS.md' 'docs/policies/README.md'
Require-Link 'AGENTS.md' 'docs/policies/development.md'
Require-Link 'AGENTS.md' 'docs/repository.md'
Require-Link 'AGENTS.md' 'docs/architecture.md'
Require-Link 'README.md' 'CONTRIBUTING.md'
Require-Link 'README.md' 'docs/policies/README.md'
Require-Link 'README.md' 'docs/repository.md'
Require-Link 'README.md' 'docs/architecture.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/README.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/development.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/branching.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/commits.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/changelog.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/testing.md'
Require-Link 'CONTRIBUTING.md' 'docs/policies/pull-requests.md'
Require-Link 'CONTRIBUTING.md' 'CHANGELOG.md'
Require-Link 'CONTRIBUTING.md' 'docs/repository.md'
Require-Link 'docs/repository.md' 'policies/README.md'
Require-Link 'docs/policies/README.md' 'development.md'
Require-Link 'docs/policies/README.md' '../repository.md'
Require-Link 'docs/policies/README.md' '../architecture.md'
Require-Link 'docs/policies/README.md' '../FEATURES.md'
$policies | ForEach-Object { Require-Link 'docs/policies/README.md' "$_.md" }
$policies | Where-Object { $_ -ne 'development' } |
    ForEach-Object { Require-Link "docs/policies/$_.md" 'development.md' }

# --- Solution items --------------------------------------------------------------------------
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
    [xml]$xml = Read-Utf8File $solution.FullName
    $listed = @($xml.SelectNodes('//File') | ForEach-Object { $_.Path.Replace('\', '/') })
} else {
    $text = Read-Utf8File $solution.FullName
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

# --- Markdown links and anchors --------------------------------------------------------------
# Every relative link must resolve to an existing file or directory, and a `#anchor` must point at
# a heading that exists in the target Markdown document.
function Get-HeadingSlug([string]$Heading) {
    $slug = $Heading.ToLowerInvariant()
    $slug = [regex]::Replace($slug, '[^a-z0-9\u0400-\u04FF _-]', '')
    $slug = $slug.Replace(' ', '-')
    return $slug.Trim('-')
}
function Get-MarkdownSlugs([string]$Path) {
    $slugs = @{}
    foreach ($line in (Read-Utf8File $Path) -split '\r?\n') {
        if ($line -match '^#{1,6}\s+(.+?)\s*#*\s*$') {
            $slug = Get-HeadingSlug $Matches[1]
            if ($slug) { $slugs[$slug] = $true }
        }
    }
    return $slugs
}

$markdownFiles = @()
$markdownFiles += @(Get-ChildItem -LiteralPath $root -File -Filter '*.md')
foreach ($folder in @('docs', '.github')) {
    $folderPath = Join-Path $root $folder
    if (Test-Path -LiteralPath $folderPath -PathType Container) {
        $markdownFiles += @(Get-ChildItem -LiteralPath $folderPath -File -Recurse -Filter '*.md' |
            Where-Object { $_.FullName -notmatch '[\\/](\.git|bin|obj|\.obsidian)[\\/]' })
    }
}
$slugsByFile = @{}
foreach ($file in $markdownFiles) { $slugsByFile[$file.FullName] = Get-MarkdownSlugs $file.FullName }
$linkPattern = '\[[^\]]*\]\(\s*<?([^)\s>]+)>?\s*\)'
$script:linkErrors = @()
foreach ($file in $markdownFiles) {
    $content = Read-Utf8File $file.FullName
    $content = [regex]::Replace($content, '(?ms)^\s*```.*?^\s*```', '')
    $content = [regex]::Replace($content, '(?ms)^\s*~~~.*?^\s*~~~', '')
    foreach ($match in [regex]::Matches($content, $linkPattern)) {
        $target = $match.Groups[1].Value
        if ($target -match '^(https?:|mailto:|#?$)') { continue }
        $targetPath, $anchor = $target -split '#', 2
        $targetPath = [uri]::UnescapeDataString($targetPath)
        if (-not $targetPath) {
            $resolved = $file.FullName
        } else {
            $resolved = [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $targetPath))
        }
        $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
        $displayTarget = $targetPath.Replace('\', '/')
        if (-not (Test-Path -LiteralPath $resolved)) {
            $script:linkErrors += "${relative}: link target not found: $displayTarget"
            continue
        }
        if (-not $anchor -or -not $resolved.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase)) { continue }
        if (-not $slugsByFile.ContainsKey($resolved)) { $slugsByFile[$resolved] = Get-MarkdownSlugs $resolved }
        $slug = $anchor.ToLowerInvariant()
        if (-not $slugsByFile[$resolved].ContainsKey($slug)) {
            $script:linkErrors += "${relative}: anchor not found: $displayTarget#$anchor"
        }
    }
}
if ($script:linkErrors) {
    throw "Broken Markdown links:`n$($script:linkErrors -join "`n")"
}

# Build output must never be tracked: generated bin/ and obj/ content is local to a machine.
if (Get-Command git -ErrorAction SilentlyContinue) {
    $trackedOutputs = @(& git -C $root ls-files |
        Where-Object { $_ -match '(^|/)(bin|obj)/' } |
        Sort-Object)
    if ($trackedOutputs) {
        throw "Tracked build output: $($trackedOutputs -join ', ')"
    }
}

Write-Host "Repository policy validation passed: $($solution.Name); $($listed.Count) solution items; $($markdownFiles.Count) Markdown files checked."
