param(
    [string]$Source = 'wiki',
    [string]$Destination = '.wiki-publish'
)

$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $Source).Path
$destinationRoot = [IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Destination))

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
    foreach ($match in [regex]::Matches($content, $wikiLinkPattern)) {
        $target = $match.Groups[1].Value
        $pageTarget = ($target -split '#', 2)[0]
        $pageName = [IO.Path]::GetFileNameWithoutExtension($pageTarget)
        if (-not $pages.ContainsKey($pageName)) {
            $relativeFile = [IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
            throw "Broken Wiki link in '$relativeFile': $target"
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
