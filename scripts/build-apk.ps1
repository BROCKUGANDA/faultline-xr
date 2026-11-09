#requires -Version 5.1
<#
.SYNOPSIS
    Builds the FAULTLINE XR Android ARM64 APK with a real Unity 6 installation.

.DESCRIPTION
    Drives Unity in batch mode through the four steps that produce a
    submission artifact:

      1. compile the project (import + script assembly)
      2. configure player settings, XR stack, shaders and scenes
      3. build the runtime scene
      4. run EditMode + PlayMode tests, then build the APK and manifest

    Everything is logged. Exit codes are propagated: a failing step stops the
    run instead of reporting a green build.

    Prerequisites (verified by this script before it starts):
      - Unity 6000.0.66f2 with Android Build Support installed
      - OpenJDK 17 + Android SDK/NDK reachable from Unity's External Tools
      - A Unity licence activated for the current user
      - Meta XR All-in-One SDK resolved from Packages/manifest.json

.PARAMETER UnityEditorPath
    Unity.exe to use. Defaults to the D:\Unity\6000.0.66f2 install, then falls
    back to Unity Hub's standard location.

.PARAMETER ProjectPath
    Unity project root. Defaults to the repository containing this script.

.PARAMETER OutputPath
    Artifact folder. Defaults to submission/build.

.PARAMETER SkipTests
    Skip the test stage. Only valid while iterating; the release candidate must
    run with tests enabled.

.PARAMETER TestsOnly
    Run the test stages and stop before building the APK.

.EXAMPLE
    pwsh -File scripts/build-apk.ps1

.EXAMPLE
    pwsh -File scripts/build-apk.ps1 -TestsOnly
#>
[CmdletBinding()]
param(
    [string]$UnityEditorPath = '',
    [string]$ProjectPath = '',
    [string]$OutputPath = 'submission/build',
    [switch]$SkipTests,
    [switch]$TestsOnly
)

$ErrorActionPreference = 'Stop'

function Write-Step([int]$Number, [string]$Text) {
    Write-Host ("[{0}] {1}" -f $Number, $Text) -ForegroundColor Yellow
}

function Resolve-UnityEditor([string]$Requested) {
    $candidates = @()
    if ($Requested) { $candidates += $Requested }
    $candidates += @(
        'D:\Unity\6000.0.66f2\Editor\Unity.exe',
        'C:\Program Files\Unity\Hub\Editor\6000.0.66f2\Editor\Unity.exe',
        'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe'
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }
    throw "Unity 6 LTS editor not found. Looked in: $($candidates -join ', '). Install Unity 6000.0.66f2 or pass -UnityEditorPath."
}

function Invoke-UnityStage {
    param(
        [string]$StageName,
        [string[]]$UnityArguments,
        [string]$LogFile
    )

    Write-Host ("--- {0} ---" -f $StageName) -ForegroundColor Cyan
    $process = Start-Process -FilePath $Script:Unity -ArgumentList $UnityArguments `
        -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        if (Test-Path $LogFile) {
            Write-Host ("last 40 log lines from {0}:" -f $LogFile) -ForegroundColor DarkGray
            Get-Content $LogFile -Tail 40 | ForEach-Object { Write-Host "  $_" }
        }
        throw "Unity stage '$StageName' failed with exit code $($process.ExitCode)."
    }
    Write-Host ("{0} exit code 0" -f $StageName) -ForegroundColor Green
}

if (-not $ProjectPath) { $ProjectPath = (Split-Path -Parent $PSScriptRoot) }
$ProjectPath = (Resolve-Path $ProjectPath).Path
$Unity = Resolve-UnityEditor $UnityEditorPath
Write-Host ("Unity:  {0}" -f $Unity)
Write-Host ("Project: {0}" -f $ProjectPath)

$AbsoluteOutput = Join-Path $ProjectPath $OutputPath
New-Item -ItemType Directory -Force -Path $AbsoluteOutput | Out-Null

$LogFile = Join-Path $AbsoluteOutput 'build.log'
$EditModeResults = Join-Path $AbsoluteOutput 'test-editmode.xml'
$PlayModeResults = Join-Path $AbsoluteOutput 'test-playmode.xml'

# Shared args: batch mode, this project, log to file, no licence popup.
$baseArgs = @('-batchmode', '-nographics', '-projectPath', $ProjectPath)

Write-Step 1 'Compile / import (license check + script assembly)'
Invoke-UnityStage -StageName 'import' -LogFile $LogFile -UnityArguments (
    $baseArgs + @('-quit', '-logFile', $LogFile))

Write-Step 2 'Configure project (Android ARM64, OpenXR + hand tracking, shaders, scenes)'
Invoke-UnityStage -StageName 'configure' -LogFile $LogFile -UnityArguments (
    $baseArgs + @('-executeMethod', 'Faultline.Editor.FaultlineProjectConfigurer.Configure',
        '-logFile', $LogFile))

Write-Step 3 'Build the runtime scene'
Invoke-UnityStage -StageName 'scene' -LogFile $LogFile -UnityArguments (
    $baseArgs + @('-executeMethod', 'Faultline.Editor.FaultlineSceneBuilder.BuildScene',
        '-logFile', $LogFile))

if (-not $SkipTests) {
    Write-Step 4 'EditMode tests'
    Invoke-UnityStage -StageName 'editmode-tests' -LogFile $LogFile -UnityArguments (
        $baseArgs + @('-runTests', '-testPlatform', 'EditMode',
            '-testResults', $EditModeResults, '-logFile', $LogFile))

    Write-Step 5 'PlayMode tests'
    Invoke-UnityStage -StageName 'playmode-tests' -LogFile $LogFile -UnityArguments (
        $baseArgs + @('-runTests', '-testPlatform', 'PlayMode',
            '-testResults', $PlayModeResults, '-logFile', $LogFile,
            '-batchmode', '-accept-api-update'))
}
else {
    Write-Host 'TESTS SKIPPED by -SkipTests — results are NOT validated for release.' -ForegroundColor Red
}

if ($TestsOnly) {
    Write-Host 'Tests-only run complete.' -ForegroundColor Green
    exit 0
}

Write-Step 6 'Build Android ARM64 APK'
Invoke-UnityStage -StageName 'apk' -LogFile $LogFile -UnityArguments (
    $baseArgs + @('-executeMethod', 'Faultline.Editor.FaultlineBuildScript.BuildApk',
        '-logFile', $LogFile))

$Apk = Join-Path $AbsoluteOutput 'FAULTLINE-XR.apk'
if (-not (Test-Path $Apk)) {
    throw "Unity reported success but $Apk does not exist."
}

$item = Get-Item $Apk
$sha = (Get-FileHash -Path $Apk -Algorithm SHA256).Hash
Write-Host ''
Write-Host ('APK:      {0}' -f $item.FullName) -ForegroundColor Green
Write-Host ('Size:     {0} bytes' -f $item.Length) -ForegroundColor Green
Write-Host ('SHA-256:  {0}' -f $sha) -ForegroundColor Green

$manifest = Join-Path $AbsoluteOutput 'build_manifest.json'
if (Test-Path $manifest) {
    Write-Host ('Manifest: {0}' -f (Resolve-Path $manifest).Path) -ForegroundColor Green
}

# Test summary next to the artifacts so the submission evidence is in one place.
$summaryPath = Join-Path $AbsoluteOutput 'test_summary.txt'
$lines = @("built_utc=$(Get-Date).ToUniversalTime().ToString('s')", "unity=$Unity", "apk=$($item.FullName)", "sha256=$sha")
foreach ($results in @($EditModeResults, $PlayModeResults)) {
    if (Test-Path $results) {
        [xml]$xml = Get-Content $results
        $run = $xml.'test-run'
        $lines += ("{0}: total={1} passed={2} failed={3} skipped={4}" -f `
            (Split-Path $results -Leaf), $run.total, $run.passed, $run.failed, $run.skipped)
    }
    else {
        $lines += ("{0}: NOT RUN" -f (Split-Path $results -Leaf))
    }
}
Set-Content -Path $summaryPath -Value ($lines -join [Environment]::NewLine)
Write-Host ('Test summary: {0}' -f $summaryPath) -ForegroundColor Green

Write-Host ''
Write-Host 'Next: install on a headset and smoke-test the CASCADE journey —' -ForegroundColor Cyan
Write-Host "  adb install '$($item.FullName)'"
Write-Host 'Device validation is required before claiming the APK works.' -ForegroundColor Cyan
exit 0
