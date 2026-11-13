# FAULTLINE XR — Deployment Status

**Date:** 2026-11-13
**Host:** Hetzner `hetzner-hermes` (135.181.83.101), Ubuntu, root SSH key auth

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

## The one remaining step (needs your Cloudflare login)

The public URL `https://faultline.aiazimara.com` requires **one dashboard action**.
There is no Cloudflare API token on this machine (wrangler OAuth expired, no
`CLOUDFLARE_API_TOKEN`), and `aiazimara.com`'s NS are `eloise/maciej.ns.cloudflare.com`,
so a DNS record can only be created by you or with a token.

**Cloudflare Dashboard → Zero Trust → Networks → Tunnels →**
tunnel `d7326277-3793-4c6e-8f87-3b64bc244fa7` **→ Public Hostname → Add**

| Field | Value |
| --- | --- |
| Subdomain | `faultline` |
| Domain | `aiazimara.com` |
| Service | `HTTP` |
| URL | `localhost:8088` |

This creates the proxied CNAME automatically. Then:

```bash
curl -sSI https://faultline.aiazimara.com/   # expect 200 text/html
```

### Alternative: give me a token and I finish it

Create a Cloudflare API token with **Zone → DNS → Edit** on `aiazimara.com` plus
**Account → Cloudflare Tunnel → Edit**, then:

```powershell
$env:CLOUDFLARE_API_TOKEN = "<token>"
# I then run: cloudflared tunnel route dns create + ingress update, and verify HTTPS
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

The tunnel path is isolated: a dedicated container, a dedicated port, and no change
to any existing service.

---

## APK

Built with the real resolved URL baked in (verified by inspecting `classes.dex`):

```
Package:  com.faultline.xr
Label:    FAULTLINE XR
minSdk:   24    targetSdk: 37
Signed:   v2 ✅  v3 ✅
Size:     12,950 bytes
SHA-256:  0BB22310A9F2A85721F1C43368DD42C23B4A0EBB24119D2C4887A4039D954FDA
URL:      https://faultline.aiazimara.com/
```

Rebuild any time:

```powershell
node scripts/build-twa-apk.mjs --host faultline.aiazimara.com --keypass <password> --out submission\build
```

The script is fully offline (aapt2 → javac → d8 → jar → zipalign → apksigner), needs
no Gradle, and fails loudly if any stage fails. It restores the source placeholder
afterwards so the tree stays clean.

**Note on the APK's nature:** this is a TWA launcher, not a native XR runtime.
Android WebView does **not** implement the WebXR Device API, so the in-app view
shows the desktop-preview flow; the "Open in VR browser" button hands off to a
WebXR-capable browser. The judged experience is the WebXR site itself — the APK is
a convenience install for review on a phone.

---

## VPS disk (fixed during this session)

The host was at **100% full (0 bytes free)** — Coolify's own Postgres had stopped
accepting connections. Reclaimed to **85% used (5.5 GB free)** by removing
unused Coolify images and rotating journals. No running service was stopped.