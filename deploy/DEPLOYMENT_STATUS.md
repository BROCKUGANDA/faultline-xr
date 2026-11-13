# FAULTLINE XR — Deployment Status

**Date:** 2026-11-13
**Live URL:** https://faultline.aiazimara.com
**Host:** Hetzner `hetzner-hermes` (135.181.83.101), Ubuntu, root SSH key auth

---

## Status: LIVE AND VERIFIED

| Check | Result |
| --- | --- |
| `https://faultline.aiazimara.com/` | ✅ **200** |
| Document is the FAULTLINE XR build | ✅ `<title>FAULTLINE XR</title>`, correct bundle hash |
| JS / CSS / healthz assets | ✅ **200 / 200 / 200** |
| Browser boots the app | ✅ 8/8 assertions (`scripts/e2e-public.mjs`) |
| No console errors, no failed requests | ✅ clean |
| Containers stable | ✅ 0 restarts, both healthy |
| TLS | ✅ Cloudflare edge certificate, HTTPS enforced |

`e2e-public.mjs` runs against the **live URL**, so DNS, the tunnel, TLS and the
deployed bundle are all validated the way a judge receives them — not just
against localhost.

---

## What is deployed and verified

| Component | Location | Status |
| --- | --- | --- |
| Production build (`web/dist`) | `/opt/faultline-xr/site` on the VPS | ✅ Uploaded, byte-verified |
| Static site container | `faultline-xr` (nginx:1.27-alpine) | ✅ **Up (healthy)** |
| Health endpoint | `http://127.0.0.1:8088/healthz` | ✅ `{"ok":true,"service":"faultline-xr","build":"1.0.0"}` |
| Asset routes | `/`, `/assets/*.js`, `/assets/*.css` | ✅ all **200** |
| Network exposure | `127.0.0.1:8088` only | ✅ **Not publicly reachable** |
| Compose | `/opt/faultline-xr/docker-compose.faultline.yml` | ✅ Committed to repo |

Commands:

```bash
cd /opt/faultline-xr
docker compose -f docker-compose.faultline.yml up -d     # deploy
curl -s http://127.0.0.1:8088/healthz                   # verify
docker compose -f docker-compose.faultline.yml down      # stop
```

---

## Cloudflare Tunnel (done — this is how the URL is live)

A **dedicated tunnel** was created so the production `plumbline` tunnel and its
ingress rules are never touched.

| Item | Value |
| --- | --- |
| Tunnel name | `faultline-xr` |
| Tunnel ID | `4f041f37-b5ff-48c6-8a79-12078c4abaa8` |
| Ingress | `faultline.aiazimara.com` → `http://faultline-xr:80` (container name) |
| Catch-all | `http_status:404` |
| DNS | `faultline.aiazimara.com` CNAME → `4f041f37-….cfargotunnel.com` (proxied) |
| Connector | `faultline-tunnel` container, same Docker network as the site |

Everything was applied through the Cloudflare API with a scoped token:

```powershell
# DNS record
POST /zones/b4ed7767a390081877401fc4dc11f9a3/dns_records
# tunnel
POST /accounts/e347bacdb10db4db992748ba5e7d91f7/cfd_tunnel
# ingress
PUT  /accounts/…/cfd_tunnel/4f041f37-…/configurations
```

Both containers share the `faultline` Docker network, so cloudflared resolves the
site by name. No host-gateway routing, no reliance on the loopback port.

### Re-deploy

```bash
cd /opt/faultline-xr
docker compose -f docker-compose.faultline.yml up -d
docker compose -f docker-compose.faultline.yml logs -f tunnel
```

---

## Why not GitHub Pages or the existing edge?

- **GitHub Pages** — verified this run: `https://brockuganda.github.io/faultline-xr/`
  returns **301 → `http://securevoiceai.me/faultline-xr/`**. The repo's Pages has
  `cname: null`, yet the account-level verified domain still hijacks every
  github.io URL. Clearing that domain is the known fix, but it belongs to another
  live product.
- **`aiazimara.com` production edge** — this is a Coolify/Traefik + Caddy stack
  serving a live Next.js site with WAF and strict security headers. Editing it to
  add a vhost risks an unrelated production outage, so I did not touch it.

The tunnel path is isolated: a dedicated tunnel, a dedicated container, a dedicated
network, and no change to any existing service.

---

## APK

Built with the real resolved URL baked in (verified by inspecting `classes.dex`):

```
Package:  com.faultline.xr
Label:    FAULTLINE XR
minSdk:   24    targetSdk: 37
Signed:   v2 ✅  v3 ✅
Size:     17,046 bytes
SHA-256:  0D4D2122CF681E90BE40F67AE805BC123E29C9968EF5F60636E178D37E64E6FC
URL:      https://faultline.aiazimara.com/
```

Rebuild any time:

```powershell
node scripts/build-twa-apk.mjs --host faultline.aiazimara.com --keypass <password> --out submission\build
```

The script is fully offline (aapt2 → javac → d8 → jar → zipalign → apksigner), needs
no Gradle, and fails loudly if any stage fails. It restores the source placeholder
afterwards so the tree stays clean.

### XR runtime reality — verified, not assumed

| Runtime | `navigator.xr` | `immersive-vr` | Notes |
| --- | --- | --- | --- |
| **Android System WebView** | ❌ **none** | ❌ | Every `navigator.xr` API is listed unsupported in WebView ([caniwebview.com](https://caniwebview.com/features/web-feature-webxr-device/)). Platform limit, not an app bug. |
| **Chrome for Android** | ✅ partial | ✅ **yes** | `immersive-vr` via Google VR / Cardboard (Chrome 79+); `immersive-ar` via ARCore (81+). |
| **Chrome on Android XR** | ✅ full | ✅ | Hand Input is the default. |
| **Meta Quest Browser** | ✅ full | ✅ | **Hand-tracked. The judged path.** |

**Consequence:** the APK cannot render immersive VR inside its own WebView. What it
does instead is hand off to **Chrome for Android** (explicit `com.android.chrome`
package intent), where a phone in a Cardboard viewer runs the experience in
immersive stereo. Quest Browser remains the hand-tracked path.

The matrix is served at `https://faultline.aiazimara.com/xr-support.json` so a
reviewer can see exactly which runtime does what.

---

## VPS storage (resolved)

**There was never a separate block-storage volume.** `/dev/sda` is 76 GB total,
partitioned so `sda3` (74.3 GB) is an LVM physical volume — but the root logical
volume had been created at only 37.14 GB at install time, leaving **37.15 GB
unallocated in the volume group**.

That free space was claimed online:

```bash
lvextend -l +100%FREE /dev/ubuntu-vg/ubuntu-lv
resize2fs /dev/ubuntu-vg/ubuntu-lv
```

| | Before | After |
| --- | --- | --- |
| Root filesystem | 37 GB (85% used, 5.3 GB free) | **73 GB (38% used, 44 GB free)** |
| Free space | 5.3 GB | **44 GB** |

No reboot, no downtime, no data movement — ext4 grew the mounted filesystem in
place. Unused Coolify images were also pruned (3.57 GB reclaimed).