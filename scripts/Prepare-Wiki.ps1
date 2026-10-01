param(
    [string]$Source = 'wiki',
    [string]$Destination = '.wiki-publish'
)

$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $Source).Path
$destinationRoot = if ([IO.Path]::IsPathFullyQualified($Destination)) {
    [IO.Path]::GetFullPath($Destination)
} else {
    [IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Destination))
}

if (Test-Path -LiteralPath $destinationRoot) {
    throw "Wiki publication directory already exists: $destinationRoot"
}

$markdownFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.md')
$pages = @{}
foreach ($file in $markdownFiles) {
    $pageName = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    if ($pages.ContainsKey($pageName)) {
        throw "Duplicate Wiki page name '$pageName': $($pages[$pageName]) and $($file.FullName)"
    }
    $pages[$pageName] = $file.FullName
}

$wikiLinkPattern = '\[\[([^\]|]+?)(?:\\?\|([^\]]+))?\]\]'
foreach ($file in $markdownFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $contentWithoutLinks = [regex]::Replace($content, $wikiLinkPattern, '')
    if ($contentWithoutLinks.Contains('[[') -or $contentWithoutLinks.Contains(']]')) {
        $relativeFile = [IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
        throw "Malformed Wiki link syntax in '$relativeFile'"
    }

    foreach ($line in $content -split '\r?\n') {
        if (-not $line.TrimStart().StartsWith('|')) { continue }
        foreach ($match in [regex]::Matches($line, $wikiLinkPattern)) {
            if ($match.Value -match '(?<!\\)\|') {
                $relativeFile = [IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
                throw "Unescaped Wiki link alias in Markdown table '$relativeFile': $($match.Value)"
            }
        }
    }

    foreach ($match in [regex]::Matches($content, $wikiLinkPattern)) {
        $target = $match.Groups[1].Value
        $pageTarget = ($target -split '#', 2)[0]
        $pageName = [IO.Path]::GetFileNameWithoutExtension($pageTarget)
        if (-not $pages.ContainsKey($pageName)) {
            $relativeFile = [IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
            throw "Broken Wiki link in '$relativeFile': $target"
        }
        if ($pageTarget -match '[/\\]') {
            $targetPath = Join-Path $sourceRoot ($pageTarget.Replace('/', [IO.Path]::DirectorySeparatorChar) + '.md')
            if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
                $relativeFile = [IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
                throw "Broken Wiki link path in '$relativeFile': $target"
            }
        }
    }
}

Copy-Item -LiteralPath $sourceRoot -Destination $destinationRoot -Recurse

Get-ChildItem -LiteralPath $destinationRoot -Recurse -File -Filter '*.md' | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $content = [regex]::Replace($content, $wikiLinkPattern, {
        param($match)

        $targetParts = $match.Groups[1].Value -split '#', 2
        $pageName = [IO.Path]::GetFileNameWithoutExtension($targetParts[0])
        if ($targetParts.Count -gt 1) {
            $pageName += "#$($targetParts[1])"
        }

        if (-not $match.Groups[2].Success) {
            return "[[$pageName]]"
        }

        $label = $match.Groups[2].Value
        return "[[$label|$pageName]]"
    })
    Set-Content -LiteralPath $_.FullName -Value $content -NoNewline -Encoding utf8
}

Write-Host "Prepared $($markdownFiles.Count) Wiki pages in $destinationRoot."
