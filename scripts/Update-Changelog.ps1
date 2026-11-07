<#
.SYNOPSIS
Verifies the pending changelog fragments, collects them under `## [Unreleased]`, or turns the
collected entries into a release section.

.DESCRIPTION
A change carries its entry as one fragment file under `changelog.d/<category>/<slug>.md`. The
category directory is the `CHANGELOG.md` heading (`added`, `changed`, `deprecated`, `removed`,
`fixed`, `security`), the file name is a lowercase kebab-case slug that describes the change, and
the file holds one line: the entry text, without the `- ` list marker and without a heading.

- `-Verify` checks the category directories, the file names and the content. The
  `repository-policy` workflow runs it on every push and pull request.
- `-Compose` writes the fragments into `## [Unreleased]` under their category headings, in category
  order and then file-name order. It refuses when `Unreleased` already has entries.
- `-Finalize <version>` turns the composed entries into `## [VERSION] - YYYY-MM-DD`, leaves a new
  empty `## [Unreleased]` above it and deletes the fragments. Deleting them on the release branch
  is what keeps a merged-back `dev` free of entries that already shipped.

The release flow around the three modes is defined by the changelog policy.

.PARAMETER RepositoryRoot
Repository root. Defaults to the current directory.

.PARAMETER Verify
Check the pending fragments and exit. Nothing is written.

.PARAMETER Compose
Collect the pending fragments into `## [Unreleased]`.

.PARAMETER Finalize
Version of the release section to create, without the leading `v`, for example `3.0.5`.

.PARAMETER Date
Date of the release section as `YYYY-MM-DD`. Defaults to today.
#>
param(
    [string]$RepositoryRoot = (Get-Location).Path,

    [switch]$Verify,

    [switch]$Compose,

    [string]$Finalize = '',

    [string]$Date = ''
)

$ErrorActionPreference = 'Stop'

$categories = [ordered]@{
    'added'      = 'Added'
    'changed'    = 'Changed'
    'deprecated' = 'Deprecated'
    'removed'    = 'Removed'
    'fixed'      = 'Fixed'
    'security'   = 'Security'
}

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$changelogPath = Join-Path $root 'CHANGELOG.md'
$fragmentsPath = Join-Path $root 'changelog.d'
$archivePath = Join-Path $root 'docs/changelog/archive.md'
$utf8 = [Text.UTF8Encoding]::new($false)

function Read-Text([string]$Path) {
    return [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
}

function Get-ChangelogFragments([string]$Root, [System.Collections.IDictionary]$Categories) {
    $problems = [Collections.Generic.List[string]]::new()
    $entries = [ordered]@{}
    foreach ($directory in $Categories.Keys) { $entries[$Categories[$directory]] = [Collections.Generic.List[object]]::new() }

    if (Test-Path -LiteralPath $Root -PathType Container) {
        foreach ($item in Get-ChildItem -LiteralPath $Root) {
            if ($item.PSIsContainer) {
                if ($Categories.Keys -cnotcontains $item.Name) {
                    $problems.Add("changelog.d/$($item.Name) is not a category directory; use one of: $($Categories.Keys -join ', ').")
                }
                continue
            }
            $problems.Add("changelog.d/$($item.Name): a fragment belongs in a category directory, not in the root of changelog.d.")
        }

        foreach ($directory in $Categories.Keys) {
            $categoryPath = Join-Path $Root $directory
            if (-not (Test-Path -LiteralPath $categoryPath -PathType Container)) { continue }
            $category = $Categories[$directory]
            foreach ($file in @(Get-ChildItem -LiteralPath $categoryPath -File | Sort-Object Name)) {
                $relative = "changelog.d/$directory/$($file.Name)"
                if ($file.Extension -ne '.md') {
                    $problems.Add("${relative}: a fragment is a Markdown file named <slug>.md.")
                    continue
                }
                if ($file.Name -cnotmatch '^[a-z0-9]+(-[a-z0-9]+)*\.md$') {
                    $problems.Add("${relative}: the file name is lowercase kebab-case, such as keep-selected-row.md.")
                    continue
                }
                $text = (Read-Text $file.FullName).Trim()
                if (-not $text) {
                    $problems.Add("${relative}: the fragment is empty.")
                    continue
                }
                if ($text -match '\r?\n') {
                    $problems.Add("${relative}: a fragment is one line; keep the entry itself short and put the reasoning in the pull request.")
                    continue
                }
                if ($text.StartsWith('- ') -or $text.StartsWith('###')) {
                    $problems.Add("${relative}: write the entry text only; the script adds the list marker and the category heading.")
                    continue
                }
                $entries[$category].Add([pscustomobject]@{ Path = $relative; Text = $text })
            }
        }
    }

    return [pscustomobject]@{ Entries = $entries; Problems = $problems }
}

function Assert-NoProblems($Problems) {
    if ($Problems.Count -gt 0) {
        throw "Changelog fragment check failed:`n - $($Problems -join "`n - ")"
    }
}

function Get-UnreleasedIndex([string[]]$Lines) {
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match '^## \[Unreleased\]\s*$') { return $index }
    }
    return -1
}

function Get-NextSectionIndex([string[]]$Lines, [int]$From) {
    for ($index = $From + 1; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match '^## ') { return $index }
    }
    return $Lines.Count
}

$modes = @()
if ($Verify) { $modes += 'Verify' }
if ($Compose) { $modes += 'Compose' }
if ($Finalize) { $modes += 'Finalize' }
if ($modes.Count -ne 1) {
    throw 'Specify exactly one of -Verify, -Compose or -Finalize <version>.'
}

$fragments = Get-ChangelogFragments $fragmentsPath $categories
$entryCount = 0
foreach ($category in $fragments.Entries.Keys) { $entryCount += $fragments.Entries[$category].Count }

if ($Verify) {
    Assert-NoProblems $fragments.Problems
    Write-Host "Changelog fragment verification passed: $entryCount pending fragment(s)."
    exit 0
}

if (-not (Test-Path -LiteralPath $changelogPath -PathType Leaf)) {
    throw 'CHANGELOG.md was not found.'
}

$raw = Read-Text $changelogPath
$newLine = if ($raw.Contains("`r`n")) { "`r`n" } else { "`n" }
$lines = @($raw -split '\r?\n')
$unreleasedIndex = Get-UnreleasedIndex $lines
if ($unreleasedIndex -lt 0) {
    throw "CHANGELOG.md has no '## [Unreleased]' section."
}

if ($Compose) {
    Assert-NoProblems $fragments.Problems
    if ($entryCount -eq 0) {
        throw 'changelog.d holds no fragment; nothing to compose.'
    }

    $nextSectionIndex = Get-NextSectionIndex $lines $unreleasedIndex
    $pending = @()
    for ($index = $unreleasedIndex + 1; $index -lt $nextSectionIndex; $index++) {
        if ($lines[$index].Trim() -and $lines[$index] -notmatch '^###\s') { $pending += $lines[$index] }
    }
    if ($pending.Count -gt 0) {
        throw "'## [Unreleased]' already has $($pending.Count) entr$(if ($pending.Count -eq 1) { 'y' } else { 'ies' }); compose from an empty 'Unreleased' and edit the collected entries instead."
    }

    $composed = [Collections.Generic.List[string]]::new()
    foreach ($category in $fragments.Entries.Keys) {
        if ($fragments.Entries[$category].Count -eq 0) { continue }
        $composed.Add('')
        $composed.Add("### $category")
        $composed.Add('')
        foreach ($entry in $fragments.Entries[$category]) { $composed.Add("- $($entry.Text)") }
    }

    $newLines = @($lines[0..$unreleasedIndex]) + @($composed) + @($lines[($unreleasedIndex + 1)..($lines.Count - 1)])
    [IO.File]::WriteAllText($changelogPath, ($newLines -join $newLine), $utf8)
    Write-Host "Composed $entryCount fragment(s) into '## [Unreleased]'. Edit the collected entries, then run -Finalize <version>."
    exit 0
}

if ($Finalize -cnotmatch '^\d+\.\d+\.\d+$') {
    throw "The release version must be MAJOR.MINOR.PATCH without the leading 'v'; got '$Finalize'."
}

$releaseDate = $Date
if (-not $releaseDate) { $releaseDate = (Get-Date).ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture) }
$parsedDate = [datetime]::MinValue
if (-not [datetime]::TryParseExact(
        $releaseDate,
        'yyyy-MM-dd',
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::None,
        [ref]$parsedDate)) {
    throw "The release date must be YYYY-MM-DD; got '$releaseDate'."
}

$sectionPattern = '^## \[' + [regex]::Escape($Finalize) + '\]'
$duplicate = @($lines | Where-Object { $_ -match $sectionPattern })
if ($duplicate.Count -gt 0) {
    throw "CHANGELOG.md already has a section for $Finalize."
}
if (Test-Path -LiteralPath $archivePath -PathType Leaf) {
    $archived = @((Read-Text $archivePath) -split '\r?\n' | Where-Object { $_ -match $sectionPattern })
    if ($archived.Count -gt 0) {
        throw "The archive docs/changelog/archive.md already has a section for $Finalize."
    }
}

$nextSectionIndex = Get-NextSectionIndex $lines $unreleasedIndex
$composedEntries = 0
for ($index = $unreleasedIndex + 1; $index -lt $nextSectionIndex; $index++) {
    if ($lines[$index] -match '^- ') { $composedEntries++ }
}
if ($composedEntries -eq 0) {
    throw "'## [Unreleased]' has no composed entry; run -Compose first."
}

$replacement = @('## [Unreleased]', '', "## [$Finalize] - $releaseDate")
$newLines = @($lines[0..($unreleasedIndex - 1)]) + $replacement + @($lines[($unreleasedIndex + 1)..($lines.Count - 1)])
[IO.File]::WriteAllText($changelogPath, ($newLines -join $newLine), $utf8)

$removed = 0
foreach ($directory in $categories.Keys) {
    $categoryPath = Join-Path $fragmentsPath $directory
    if (-not (Test-Path -LiteralPath $categoryPath -PathType Container)) { continue }
    foreach ($file in @(Get-ChildItem -LiteralPath $categoryPath -File -Filter '*.md')) {
        Remove-Item -LiteralPath $file.FullName
        $removed++
    }
}

Write-Host "Finalized $Finalize with $composedEntries entr$(if ($composedEntries -eq 1) { 'y' } else { 'ies' }) and removed $removed fragment(s)."
