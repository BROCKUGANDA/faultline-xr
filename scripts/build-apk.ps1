#requires -Version 5.1
<#
.SYNOPSIS
    Build FAULTLINE XR Android ARM64 APK for submission.

.DESCRIPTION
    Documents and (optionally) executes the Android ARM64 APK build for
    FAULTLINE XR. Unity 6 LTS is not installed in this environment, so the
    actual build steps are commented out — this script records the intended
    procedure and produces no APK on its own.

    Prerequisites:
      - Unity 6 LTS + Android Build Support + OpenXR (via Unity Hub)
      - JDK 17 (Android SDK already present in the environment)
      - Meta XR SDK All-in-One v207+
      - This repository opened as a Unity project

    Steps:
      1. Open project in Unity Hub.
      2. Import Meta XR SDK All-in-One v207+.
      3. Configure Player settings (bundle ID, min API 24, ARM64, OpenXR).
      4. Include all scenes.
      5. Build → Android → APK.

    Output:
      - submission/build/FAULTLINE-XR.apk
      - submission/build/build_manifest.json (version, git commit, timestamp, sha-256)
      - submission/build/build.log
      - submission/build/test_summary.txt

.PARAMETER ProjectPath
    Path to the Unity project (default: current directory).

.PARAMETER OutputPath
    Output folder for the APK and build artifacts (default: ./submission/build).

.PARAMETER BundleIdentifier
    Android bundle identifier (default: com.faultline.xr).

.PARAMETER UnityEditorPath
    Path to the Unity Editor executable (optional). When omitted, build
    execution is skipped and only the documented steps are printed.

.NOTES
    PHASE 15 deliverable. This script is documentation-first: the execution
    block is commented out because Unity is not installed. No APK is claimed
    to be built by running this file in this environment.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = ".",
    [string]$OutputPath = "submission/build",
    [string]$BundleIdentifier = "com.faultline.xr",
    [string]$UnityEditorPath = $null
)

$ErrorActionPreference = "Stop"
$Script:BuildSucceeded = $false

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Section([string]$Title) {
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Write-Step([int]$N, [string]$Text) {
    Write-Host ("[Step {0}] {1}" -f $N, $Text) -ForegroundColor Yellow
}

function Write-Commented([string]$Text) {
    Write-Host ("# " + $Text) -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# 1. Prerequisites
# ---------------------------------------------------------------------------
Write-Section "FAULTLINE XR — Android ARM64 APK Build (documented)"

Write-Section "Prerequisites"
Write-Step 1 "Unity 6 LTS (with Android Build Support, OpenJDK 17, Android SDK & NDK Tools)"
Write-Commented "Install via Unity Hub → Installs → Add Editor → Unity 6 LTS."
Write-Commented "Add modules: Android Build Support, Android SDK & NDK Tools, OpenJDK."
Write-Step 2 "Android SDK already present in this environment (per environment report)"
Write-Step 3 "Meta XR SDK All-in-One v207+ package"
Write-Commented "Download from Meta for Developers / Unity Asset Store; import into project."
Write-Step 4 "Target device: Meta Quest / Meta VR Glasses (ARM64) or ADB over network"

# ---------------------------------------------------------------------------
# 2. Steps (documented; execution commented out)
# ---------------------------------------------------------------------------
Write-Section "Build Steps"

Write-Step 1 "Open this project in Unity Hub"
Write-Commented "Unity Hub → Projects → Add → select '{0}'" -f $ProjectPath
Write-Commented "Unity imports Assets/ and Packages/ on first open."

Write-Step 2 "Import Meta XR SDK All-in-One v207+"
Write-Commented "Assets → Import Package → Custom Package (or Package Manager)."
Write-Commented "Confirm the Meta XR plugin appears under Packages/ or Assets/."

Write-Step 3 "Configure XR Plug-in Management (Android → OpenXR)"
Write-Commented "Edit → Project Settings → XR Plug-in Management → Android → enable OpenXR."
Write-Commented "OpenXR → Plug-ins → enable Meta Oculus XR Plugin."
Write-Commented "Interaction Profile: Meta Quest hands/controllers."

Write-Step 4 "Configure Player settings (Android)"
Write-Commented "Edit → Project Settings → Player → Android:"
Write-Commented "  - Bundle Identifier: {0}" -f $BundleIdentifier
Write-Commented "  - Minimum API Level: 24 (Android 7.0; per Meta XR SDK requirement use 29+ if needed)"
Write-Commented "  - Target Architectures: ARM64"
Write-Commented "  - XR Settings: Virtual Reality Supported; OpenXR listed"
Write-Commented "  - Other Settings: Company Name / Product Name"

Write-Step 5 "Include all scenes"
Write-Commented "File → Build Settings → Add Open Scenes (ensure every scenario/HUD scene is listed)."

Write-Step 6 "Build → Android → APK"
Write-Commented "File → Build Settings → Android → Switch Platform."
Write-Commented "Build Format: APK. Build System: Gradle."
Write-Commented "Click Build → choose '{0}'." -f $OutputPath

# ---------------------------------------------------------------------------
# 3. Execution (commented out — Unity not installed)
# ---------------------------------------------------------------------------
Write-Section "Execution (commented out — Unity not installed in this environment)"

$unityArgs = @(
    "-quit",
    "-batchmode",
    "-projectPath", (Resolve-Path $ProjectPath).Path,
    "-executeMethod", "Faultline.Build.BuildApk",
    "-logFile", (Join-Path $OutputPath "build.log")
)

Write-Commented "# The following block is intentionally NOT executed."
Write-Commented "# Uncomment and implement Faultline.Build.BuildApk once Unity 6 LTS is installed."
Write-Commented "# & '$UnityEditorPath' @unityArgs"

# Example (commented):
# if ($UnityEditorPath) {
#     Write-Host "Executing Unity build..." -ForegroundColor Cyan
#     & $UnityEditorPath @unityArgs
#     if ($LASTEXITCODE -ne 0) {
#         Write-Error "Unity build failed with exit code $LASTEXITCODE"
#     }
#     $Script:BuildSucceeded = $true
# } else {
#     Write-Host "UnityEditorPath not supplied — skipping build execution (documentation only)." -ForegroundColor Yellow
# }

Write-Host ""
Write-Host "Unity is not installed; no build will be executed." -ForegroundColor Yellow
Write-Host "Review the steps above and run them manually in the Unity Editor." -ForegroundColor Yellow

# ---------------------------------------------------------------------------
# 4. Output artifacts
# ---------------------------------------------------------------------------
Write-Section "Output Artifacts"
Write-Step 1 "submission/build/FAULTLINE-XR.apk — the Android ARM64 APK"
Write-Step 2 "submission/build/build_manifest.json — version, gitCommit, buildTimestamp, targetPlatform, apkPath, sha256Checksum, unityVersion, metaXrSdkVersion, testSummary"
Write-Step 3 "submission/build/build.log — full Unity build log"
Write-Step 4 "submission/build/test_summary.txt — test results summary (EditMode + PlayMode)"

# ---------------------------------------------------------------------------
# 5. Verification commands
# ---------------------------------------------------------------------------
Write-Section "Verification Commands"
Write-Commented "# Confirm APK exists and is non-empty"
Write-Commented "Test-Path submission/build/FAULTLINE-XR.apk"
Write-Commented "# Install on headset"
Write-Commented "adb install submission/build/FAULTLINE-XR.apk"
Write-Commented "# Or Build & Run from Unity"
Write-Commented "# Compute SHA-256 checksum (for build_manifest.json)"
Write-Commented "Get-FileHash submission/build/FAULTLINE-XR.apk -Algorithm SHA256"
Write-Commented "# Launch scenario HUD and verify hand tracking is active"

exit 0
