param([string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $projectDir 'Logs'
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
$references = @()
$references += Get-ChildItem -LiteralPath (Join-Path $UnityData 'NetStandard\ref\2.1.0') -Filter '*.dll'
$references += Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed\UnityEngine') -Filter '*.dll'
$references += Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed') -Filter 'UnityEditor*.dll' | Where-Object { $_.Name -ne 'UnityEditor.dll' }
$sdkRoot = Join-Path (Split-Path -Parent (Get-Command dotnet).Source) 'sdk'
$sdk = Get-ChildItem -LiteralPath $sdkRoot -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'Roslyn\bincore\csc.dll') } | Sort-Object Name -Descending | Select-Object -First 1
$compiler = Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll'
$argsFile = Join-Path $logsDir 'csharp-validation.rsp'
$lines = @('/nostdlib+','/target:library','/langversion:9.0','/define:UNITY_EDITOR,UNITY_6000_0_OR_NEWER,ENABLE_LEGACY_INPUT_MANAGER',('/out:"' + (Join-Path $logsDir 'Getaway.Validation.dll') + '"'))
$lines += $references | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$lines += Get-ChildItem -LiteralPath (Join-Path $projectDir 'Assets\Getaway') -Recurse -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
$lines | Set-Content -LiteralPath $argsFile -Encoding UTF8
& dotnet $compiler ('@' + $argsFile)
if ($LASTEXITCODE -ne 0) { throw 'C# reference compilation failed.' }
Write-Output 'C# source/API reference check passed. This does not run Unity or compile shaders.'
