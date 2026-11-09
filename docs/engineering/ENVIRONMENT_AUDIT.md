# FAULTLINE XR — Environment Audit (Phase 0 Reconnaissance)

**Project:** FAULTLINE XR (Meta Quest / Meta VR Glasses, Unity + C# + OpenXR + Meta XR SDK)
**Working directory:** `D:/AI AZIMARA(AAA)/FAULTLINE XR/`
**Audit date:** 2026-11-08
**Scope:** Inspection only — no installs, no Unity packages, no game code created.

---

## 1. Directory & Git Status

| Check | Result |
|-------|--------|
| Current directory | `C:\Users\HP\tmp` (shell cwd) |
| FAULTLINE XR directory | `D:/AI AZIMARA(AAA)/FAULTLINE XR/` — EXISTS, EMPTY (no files, no subdirectories) |
| Is a git repo? | **No** — `git rev-parse --is-inside-work-tree` → `fatal: not a git repository` |
| `git status` | Not applicable (not a repo) |
| `git log --oneline -5` | Not applicable (not a repo) |

The project is greenfield: the directory exists but contains nothing, and it is not under version control.

---

## 2. OS / Shell

| Field | Value |
|-------|-------|
| OS | Windows 11, build 10.0.26300 (win32, x86_64) |
| Shell | PowerShell 7.6.9 (`pwsh.exe`) available; Windows PowerShell 5.1 (7.5.4) also present; bash via Git/MSYS2 available |
| PATH notes | Anaconda, Node (scoop + portable), Go, dotnet, Git, Flutter, and other dev tools on PATH |

---

## 3. CPU / Memory / Disk

| Field | Value |
|-------|-------|
| CPU | 12th Gen Intel(R) Core(TM) i7-1255U, 10 cores / 12 logical processors, max clock 1700 MHz |
| Architecture | x86_64 |
| Total physical memory | 16,858,472,448 bytes ≈ **15.7 GB** |
| Disk C: | 953 GB total, 942 GB used, **12 GB free** (99% full — constrained) |
| Disk D: | 80 GB total, 55 GB used, **26 GB free** (69%) — project target drive |

> Note: C: drive is nearly full (12 GB free). Unity + Android toolchain installs should target D: where possible.

---

## 4. Toolchain Versions

| Tool | Version | Status |
|------|---------|--------|
| git | 2.55.0.windows.3 | found |
| dotnet | 10.0.403 | found |
| java / javac | — | **not found** (no JDK on PATH) |
| node | v26.7.0 | found |
| bun | 1.4.2 | found |
| python | 3.13.9 / 3.13.5 | found |
| Unity Editor | — | **not found** |
| Android SDK (ANDROID_HOME) | `C:\Users\HP\AppData\Local\Android\sdk` | found |
| Android platform-tools | 37.0.1-15733141 (adb 1.0.41) | found |
| Android build-tools | 35.0.0, 36.0.0, 37.0.0 | found |
| Android NDK | ndk/28.2.13676358 (release r28c) | found (ndk-bundle symlink absent) |
| OpenXR runtime | — | **not found** (registry key `HKLM\SOFTWARE\Khronos\OpenXR\1` absent) |
| Meta XR SDK | — | **not found** (no `.unitypackage`, no Unity cache) |
| Meta CLI / Simulator | — | **not found** (`meta`/`ovr` not on PATH) |

**Android SDK details:**
- `ANDROID_HOME` = `ANDROID_SDK_ROOT` = `C:\Users\HP\AppData\Local\Android\sdk`
- `adb` = Android Debug Bridge 1.0.41, Version 37.0.1-15733141
- NDK revision `28.2.13676358`, base revision `28.2.13676358`, release name `r28c` (located at `.../ndk/28.2.13676358/`; no `ndk-bundle` directory)

---

## 5. OpenXR / Meta XR

| Item | Result |
|------|--------|
| OpenXR runtime installed | **No** — registry key and runtime directories absent |
| Meta XR SDK available locally | **No** — no `.unitypackage` files, no Unity package cache under `AppData/Local/Unity` or `AppData/Roaming/Unity` |
| Meta XR Simulator | **No** — not found |
| Meta VR CLI (`meta`) | **No** — not on PATH |

---

## 6. Missing Prerequisites

| # | Prerequisite | Install method | Verification command |
|---|--------------|----------------|----------------------|
| 1 | Unity Editor (LTS) with Android Build Support + OpenXR modules | Install [Unity Hub](https://unity.com/download) from unity.com, then add an LTS release (e.g. Unity 6000.x LTS) with Android Build Support and OpenXR packages. Target install on D:. | `Unity -version` (from PowerShell) |
| 2 | JDK 17 | Install Adoptium/Eclipse Temurin JDK 17 or Oracle JDK 17; add `bin` to PATH. | `javac -version` |
| 3 | OpenXR Runtime + Meta XR SDK | Import Meta XR SDK All-in-One (v207+) into the Unity project via Unity Package Manager/OpenUPM; it bundles the OpenXR plugin. | Confirm `com.meta.xr.sdk.all_in_one` and OpenXR plugin in Unity Package Manager |
| 4 | Meta XR Simulator / Meta CLI | Install Meta XR Simulator package and/or Meta CLI from the Meta developer portal. | Simulator appears in Unity; `meta --version` |

Android SDK, NDK, build-tools, and adb are already present — no action needed there.

---

## 7. Recommended Version Choices (per PRD)

| Item | Recommended version | Rationale |
|------|---------------------|-----------|
| Unity | Latest LTS (e.g. Unity 6000.x LTS) | Stable, long-term support; matches PRD "Unity LTS" |
| Meta XR SDK | Meta XR SDK All-in-One **v207+** (minimum **v81+** per PRD) | All-in-One bundles OpenXR plugin; do NOT downgrade or mix incompatible versions |
| Android NDK | r28c (already installed) | Compatible with current toolchain |
| JDK | 17 | Required by Unity/Android builds |

**Important:** Do not downgrade Unity or Meta XR SDK, and do not mix versions from different release lines. Install Unity LTS first, then the Meta XR SDK All-in-One v207+ into that project.

---

## 8. Notes

- FAULTLINE XR directory exists and is empty; not a git repo. Initialize git (`git init`) when ready to version control.
- C: drive has only 12 GB free — prefer D: for Unity Editor, Android SDK caches, and project assets.
- JDK is missing and is required before Unity Android builds can succeed.
- OpenXR runtime and Meta XR SDK are not present locally; they will be pulled into the Unity project via Package Manager rather than installed system-wide.
- Node (v26.7.0), bun (1.4.2), dotnet (10.0.403), Python (3.13.x), and git are available but not required for the Unity/XR core toolchain.

---

## 9. Phase 0 Re-audit — 2026-11-10 (this run)

The 2026-11-08 audit above is **partially stale**. Re-verified facts:

| Check | 2026-11-08 | 2026-11-10 (verified) |
|-------|------------|----------------------|
| Git repository | not a repo | **Repo exists**, remote `origin` = github.com/BROCKUGANDA/faultline-xr, branch `main`, HEAD `71c393f` |
| Working tree | greenfield | **48 modified/untracked paths** at start of run (WebXR hardening + Unity reference fixes + audit artifacts); committed during this run (see git log) |
| Unity Editor | not found | **Still not installed.** Unity Hub present at `D:\UnityHub\Unity Hub.exe` (licensing client only); `C:\Program Files\Unity\Hub\Editor` is empty; no `Unity.exe` anywhere; Unity Hub requires account sign-in + ~8–10 GB install — **BLOCKED, needs user action** |
| `ProjectSettings/ProjectVersion.txt` | absent | **Created**: Unity `6000.0.66f2` (b20bc5da3050) — project file only, cannot compile without the Editor |
| `Packages/manifest.json` | absent | **Created**: `com.meta.xr.sdk.all` 207.0.1, `com.unity.xr.meta-openxr` 2.7.0, `com.unity.xr.openxr` 1.19.0, `com.unity.xr.hands` 1.7.2, URP 17.0.4, test-framework 1.4.5 — dependency declarations only, never resolved/imported |
| JDK | missing | **Present**: Eclipse Temurin 17.0.9+9 at `D:\Unity\openjdk-17.0.9` (PATH not set; Unity can be pointed at it) |
| Node / npm | v26.7.0 / 11.19.0 | v26.7.0 / 11.19.0 — `npm test`, `npm run build` re-verified (see below) |
| Android SDK | present | present at `C:\Users\HP\AppData\Local\Android\sdk` (platform-tools 37.0.1, build-tools 35/36/37, NDK 28.2.13676358 r28c) |
| Disk free | C: 12 GB, D: 26 GB | **C: 7 GB, D: 16 GB** — Unity Editor install would nearly exhaust D:; high risk |
| OpenXR runtime / Meta XR Simulator | absent | absent — unchanged |
| Submission path | Unity (assumed) | **WebXR static build** (`web/`) per `IMPLEMENTATION_STATUS.md`, `README.md`, `docs/engineering/DECISIONS.md` and `audit-2026-11-09/AUDIT_REPORT.md`; the Unity/C# tree is legacy reference, not the submission |

### Verified in this run (commands executed)

| Command | Result |
|---------|--------|
| `npm test -- --run` (web/) | **15/15 pass** after SaveData test fix (was 13/14 — see REQUIREMENT_TRACEABILITY) |
| `npx tsc --noEmit` via `npm run build` | passes |
| `npm run build` (web/) | passes; `dist/index.html` 1.87 kB, `dist/assets/index-*.js` 610.26 kB (156.93 kB gzip) |
| Headless Chromium smoke of `web/dist/` (this run) | 0 console errors, 0 page errors; desktop-preview journey walked click-by-click (evidence: `audit-2026-11-10/`) |
| Quest Browser device test | **NOT RUN — blocked, headset required (user action)** |
| Unity compile / APK / PlayMode tests | **NOT RUN — Unity Editor not installed** |

### Environment blockers needing user action

1. **Unity Editor 6000.0.66f2** install + license activation (Unity Hub sign-in) if the Unity path is ever to be compiled. Disk: D: holds only 16 GB — recommend ≥ 30 GB free first.
2. **Meta Quest headset** in Quest Browser for device verification (official Start FAQ does not require one to enter, but device behaviour is unverified).
3. **Public HTTPS deployment** (GitHub Pages self-hosted runner `faultline-xr` must be online, or another HTTPS host) — no URL is recorded anywhere in the repo; `submission/BUILD_MANIFEST.json` still contains `<PUBLIC_HTTPS_URL>`.
4. **Demo video upload** (`video/demo/out/faultline-xr-demo.mp4`, 60,252,317 bytes, ~115 s) — user action (YouTube unlisted acceptable).
5. **Devpost submission** — user account action.

**Machine-readable counterpart:** `docs/engineering/environment-report.json` (2026-11-08 snapshot; the 2026-11-10 column above is authoritative)
