param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe',
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $projectDir 'Logs'
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
if (-not (Test-Path -LiteralPath $Unity)) { throw "Unity not found: $Unity" }
function Run-Unity([string]$Method, [string]$LogName, [bool]$Quit) {
    $argsList = @('-batchmode','-nographics','-projectPath',('"' + $projectDir + '"'),'-executeMethod',$Method,'-logFile',('"' + (Join-Path $logsDir $LogName) + '"'))
    if ($Quit) { $argsList += '-quit' }
    $process = Start-Process -FilePath $Unity -ArgumentList $argsList -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity failed ($($process.ExitCode)). See Logs/$LogName" }
}
if (-not (Test-Path (Join-Path $projectDir 'Assets/Getaway/Scenes/Getaway.unity'))) {
    Run-Unity 'Getaway.Editor.ProjectSetup.CreateDemo' 'setup.log' $true
}
Run-Unity 'Getaway.Editor.SmokeTests.Run' 'smoke-tests.log' $false
if ($Build) { Run-Unity 'Getaway.Editor.ProjectSetup.BuildWindows' 'build.log' $true }
Write-Output 'Unity validation completed.'
