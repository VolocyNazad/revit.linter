<#
.SYNOPSIS
Verifies the release inputs: the CHANGELOG structure and, with -FromHeadTag, the release tag.

.DESCRIPTION
Checks `CHANGELOG.md` against the changelog policy: `## [Unreleased]` is the first section and is
empty of entries, every release has one `## [VERSION] - YYYY-MM-DD` heading, versions are unique and
ordered newest first, and dates are valid.

With `-FromHeadTag` the script additionally requires exactly one stable `vMAJOR.MINOR.PATCH` tag on
`HEAD`, requires the changelog to contain the section for that tag directly below `Unreleased`, and
optionally requires `HEAD` to be an ancestor of the given remote branch.

The repository policy workflow runs the structural checks on every push and pull request, so a broken
changelog is reported before the immutable release tag is created. The publish workflow runs the tag
checks before building the release:

    ./scripts/Verify-Release.ps1 -FromHeadTag -AncestorOf main

.PARAMETER RepositoryRoot
Repository root. Defaults to the current directory.

.PARAMETER FromHeadTag
Derive the version from the single stable tag on `HEAD` and verify the tag and the matching section.

.PARAMETER AncestorOf
Remote branch name, for example `main`. With -FromHeadTag, `HEAD` must be an ancestor of
`origin/<AncestorOf>`.
#>
param(
    [string]$RepositoryRoot = (Get-Location).Path,

    [switch]$FromHeadTag,

    [string]$AncestorOf = ''
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$changelogPath = Join-Path $root 'CHANGELOG.md'
if (-not (Test-Path -LiteralPath $changelogPath -PathType Leaf)) {
    throw 'CHANGELOG.md was not found.'
}

$problems = [Collections.Generic.List[string]]::new()

$headingPattern = '^## \[(?<label>[^\]]+)\](?: - (?<date>\d{4}-\d{2}-\d{2}))?\s*$'
$versionPattern = '^\d+\.\d+\.\d+$'
$lines = @(Get-Content -LiteralPath $changelogPath -Encoding utf8)

$headings = [Collections.Generic.List[object]]::new()
for ($index = 0; $index -lt $lines.Count; $index++) {
    if ($lines[$index] -match $headingPattern) {
        $headings.Add([pscustomobject]@{
            Index = $index
            Label = $Matches['label']
            Date = $Matches['date']
        })
    }
}

if ($headings.Count -eq 0) {
    throw "CHANGELOG.md has no '## [...]' section."
}

$firstHeading = $headings[0]
if ($firstHeading.Label -ne 'Unreleased') {
    $problems.Add("The first section must be '## [Unreleased]'; found '## [$($firstHeading.Label)]'.")
}

$unreleasedHeadings = @($headings | Where-Object Label -eq 'Unreleased')
if ($unreleasedHeadings.Count -ne 1) {
    $problems.Add("Expected exactly one '## [Unreleased]' section; found $($unreleasedHeadings.Count).")
}

if ($unreleasedHeadings.Count -ge 1) {
    $unreleased = $unreleasedHeadings[0]
    $nextHeadingIndex = if ($headings.Count -gt 1) { $headings[1].Index } else { $lines.Count }
    $pending = @()
    for ($index = $unreleased.Index + 1; $index -lt $nextHeadingIndex; $index++) {
        if ($lines[$index].Trim() -and $lines[$index] -notmatch '^###\s') {
            $pending += $lines[$index]
        }
    }
    if ($pending.Count -gt 0) {
        $problems.Add("'## [Unreleased]' still has $($pending.Count) unreleased line(s); move them into the release section before publishing.")
    }
}

$releases = @($headings | Where-Object Label -ne 'Unreleased')
$seenVersions = @{}
$previousVersion = $null
foreach ($release in $releases) {
    if ($release.Label -notmatch $versionPattern) {
        $problems.Add("Release section '## [$($release.Label)]' must name a MAJOR.MINOR.PATCH version.")
        continue
    }
    if (-not $release.Date) {
        $problems.Add("Release section '## [$($release.Label)]' has no '- YYYY-MM-DD' date.")
    } else {
        $date = [datetime]::MinValue
        if (-not [datetime]::TryParseExact(
                $release.Date,
                'yyyy-MM-dd',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::None,
                [ref]$date)) {
            $problems.Add("Release section '## [$($release.Label)]' has an invalid date '$($release.Date)'.")
        }
    }
    if ($seenVersions.ContainsKey($release.Label)) {
        $problems.Add("Release version '$($release.Label)' appears more than once.")
    } else {
        $seenVersions[$release.Label] = $true
    }
    $version = $null
    if ([version]::TryParse($release.Label, [ref]$version)) {
        if ($previousVersion -and $version -ge $previousVersion) {
            $problems.Add("Release sections must be ordered newest first; '## [$($release.Label)]' is not below the previous version.")
        }
        $previousVersion = $version
    }
}

$tag = $null
$version = $null
if ($FromHeadTag) {
    $tags = @(& git -C $root tag --points-at HEAD 2>$null | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' })
    if ($LASTEXITCODE -ne 0) {
        throw 'git tag failed; the release tag cannot be verified.'
    }
    if ($tags.Count -ne 1) {
        $problems.Add("The selected commit must carry exactly one stable tag such as 'v1.4.0'; found $($tags.Count).")
    } else {
        $tag = $tags[0]
        $version = $tag.Substring(1)
        $matching = @($releases | Where-Object Label -eq $version)
        if ($matching.Count -ne 1) {
            $problems.Add("CHANGELOG.md must contain exactly one '## [$version] - YYYY-MM-DD' section for the tag $tag; found $($matching.Count).")
        } elseif ($releases.Count -gt 0 -and $matching[0] -ne $releases[0]) {
            $problems.Add("The '## [$version]' section must be the first release section, directly below '## [Unreleased]'.")
        }
    }

    if ($AncestorOf) {
        $ancestorRef = "origin/$AncestorOf"
        & git -C $root rev-parse --verify --quiet $ancestorRef 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $problems.Add("The '$ancestorRef' ref is not available; fetch the full history before publishing.")
        } else {
            & git -C $root merge-base --is-ancestor HEAD $ancestorRef 2>$null
            if ($LASTEXITCODE -ne 0) {
                $problems.Add("HEAD is not an ancestor of '$ancestorRef'; a release is tagged on '$AncestorOf'.")
            }
        }
    }
}

if ($problems.Count -gt 0) {
    throw "Release verification failed:`n - $($problems -join "`n - ")"
}

if ($FromHeadTag) {
    Write-Host "Release verification passed for tag $tag (version $version)."
} else {
    Write-Host "Release verification passed: $($releases.Count) release section(s); 'Unreleased' is empty."
}
