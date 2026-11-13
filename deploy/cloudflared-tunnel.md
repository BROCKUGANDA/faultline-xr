# FAULTLINE XR — Cloudflare Tunnel ingress

The existing tunnel on this host is authenticated with a **connector token**
only (no `cert.pem`, no local `config.yml`). Its ingress rules therefore live in
the Cloudflare dashboard and cannot be edited from the server.

## What to add (one-time, ~60 seconds)

**Cloudflare Dashboard → Zero Trust → Networks → Tunnels →**
tunnel `d7326277-3793-4c6e-8f87-3b64bc244fa7` **→ Public Hostname → Add**

| Field | Value |
| --- | --- |
| Subdomain | `faultline` |
| Domain | `aiazimara.com` |
| Path | *(empty)* |
| Service | `HTTP` |
| URL | `localhost:8088` |

Adding the hostname creates the DNS record automatically:
`faultline.aiazimara.com → d7326277-…cfargotunnel.com` (proxied, orange cloud).
No manual DNS edit is required.

## Why this path

- `github.io` for this account 301-redirects to an unrelated domain, so the
  already-built Pages site cannot be used as-is.
- The production edge (`aiazimara.com`) is a Coolify/Traefik + Caddy stack
  serving a live Next.js site; changing it risks an unrelated production
  outage.
- The tunnel is already running and healthy, so routing a new hostname through
  it touches nothing else on the host.

## Verify

```bash
curl -sSI https://faultline.aiazimara.com/
# expect: HTTP/2 200, content-type: text/html, server: cloudflare
curl -s https://faultline.aiazimara.com/ | head -c 200
# expect: the FAULTLINE XR boot overlay markup
```

## Container side

```bash
cd /opt/faultline-xr
docker compose -f docker-compose.faultline.yml up -d
curl -s http://127.0.0.1:8088/healthz
```