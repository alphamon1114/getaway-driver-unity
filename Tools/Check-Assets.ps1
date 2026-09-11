$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$assetsDir = Join-Path $projectDir 'Assets'
$known = @{}
foreach ($meta in Get-ChildItem -LiteralPath $assetsDir -Recurse -Filter '*.meta') {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})')
    if (-not $match.Success) { throw "Missing GUID: $($meta.Name)" }
    $guid = $match.Groups[1].Value
    if ($known.ContainsKey($guid)) { throw "Duplicate GUID: $guid" }
    $known[$guid] = $meta.FullName
}
$referenceCount = 0
foreach ($file in Get-ChildItem -LiteralPath $assetsDir -Recurse -File | Where-Object { $_.Extension -ne '.meta' }) {
    if (-not (Test-Path -LiteralPath ($file.FullName + '.meta'))) { throw "Missing meta: $($file.Name)" }
    if ($file.Extension -in @('.unity','.asset','.prefab')) {
        foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), 'guid: ([a-f0-9]{32})')) {
            $guid = $match.Groups[1].Value
            if (-not $guid.StartsWith('0000000000000000') -and -not $known.ContainsKey($guid)) { throw "Broken asset reference in $($file.Name)" }
            $referenceCount++
        }
    }
}
$report = "$($known.Count) unique asset GUIDs; $referenceCount asset references resolved. PASS."
$reportDir = Join-Path $projectDir 'Docs\Validation'
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
$report | Set-Content -LiteralPath (Join-Path $reportDir 'career-assets.txt') -Encoding UTF8
Write-Output $report
