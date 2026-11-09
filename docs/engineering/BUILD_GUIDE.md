# Build Guide — Android ARM64 APK — FAULTLINE XR

Run these steps once Unity is installed. PHASE 1 creates only the scaffolding; no build is possible yet.

## Prerequisites

- Windows 10/11 host (this repo)
- Unity Hub installed
- Unity 6 LTS (with Android Build Support, OpenJDK/JDK, Android NDK, SDK & Tools)
- Meta XR SDK All-in-One v207+ package
- A Meta Quest / Meta VR Glasses headset (ARM64) for testing, or ADB over network

## Step 1 — Install Unity

1. Open Unity Hub → Installs → Add Editor.
2. Select Unity 6 LTS.
3. Add modules: Android Build Support, Android SDK & NDK Tools, OpenJDK.
4. Complete install.

## Step 2 — Open the project

1. Unity Hub → Projects → Add.
2. Select `D:/AI AZIMARA(AAA)/FAULTLINE XR/`.
3. Open the project. Unity will import `Assets/` and `Packages/`.

## Step 3 — Import Meta XR SDK

1. Download Meta XR SDK All-in-One v207+ from the Meta for Developers site / Unity Asset Store.
2. Import into the project (Assets → Import Package → Custom Package, or via Package Manager).
3. Confirm the Meta XR plugin appears under `Packages/` or `Assets/`.

## Step 4 — Enable OpenXR

1. Edit → Project Settings → XR Plug-in Management.
2. Enable **Android** → check **OpenXR**.
3. Under OpenXR → Plug-ins, enable **Meta Oculus XR Plugin**.
4. Set Interaction Profile to Meta Quest hands/controllers as needed.

## Step 5 — Configure Player settings

1. Edit → Project Settings → Player → Android.
2. **Bundle Identifier:** `com.faultline.xr` (or your org ID).
3. **Package Name:** match bundle identifier.
4. **Minimum API Level:** Android 10 (API 29) or higher per Meta XR SDK requirements.
5. **Target Architectures:** ARM64 only.
6. **XR Settings:** enable Virtual Reality Supported; OpenXR listed.
7. **Other Settings:** set appropriate Company Name / Product Name.

## Step 6 — Build the APK

1. File → Build Settings.
2. Select **Android** → click **Switch Platform**.
3. **Build Format:** APK (recommended for submission; App Bundle for store).
4. **Build System:** Gradle (preferred) or Internal.
5. Click **Build** → choose output folder (e.g. `Build/`).
6. Unity compiles C# assemblies (including the .asmdef modules) and produces `Faultline.apk`.

## Step 7 — Install on headset

1. Connect headset via USB (or enable ADB over Wi-Fi).
2. `adb install Faultline.apk` — or use Unity Build & Run.
3. Launch the app from the headset library; verify the scenario HUD appears and hand tracking is active.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Build fails: JDK not found | Install OpenJDK via Unity Hub; point JDK path in Player settings |
| OpenXR not listed | Re-import Meta XR SDK; restart Editor |
| Hand tracking not working | Confirm Meta Oculus XR Plugin enabled; request hand tracking permission at runtime |
| APK won't install | Confirm ARM64 architecture; enable headset Developer Mode + USB debugging |

## Reference

- `docs/architecture/SYSTEM_ARCHITECTURE.md` — module layout
- `docs/design/INTERACTION_GUIDELINES.md` — hand gesture contract
- `scripts/build-apk.ps1` — placeholder script mirroring these steps