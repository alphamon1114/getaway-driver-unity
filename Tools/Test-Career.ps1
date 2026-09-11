$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $projectDir 'Logs\dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:NUGET_PACKAGES = Join-Path $projectDir 'Logs\nuget'
$testDir = Join-Path $projectDir ('Logs\career-tests\' + [Guid]::NewGuid().ToString('N'))
$report = Join-Path $projectDir 'Docs\Validation\career-tests.txt'
dotnet run --project (Join-Path $projectDir 'Tests\CareerRules.Tests') -p:UseSharedCompilation=false -- $testDir $report
if ($LASTEXITCODE -ne 0) { throw 'Career rule or storage tests failed.' }
