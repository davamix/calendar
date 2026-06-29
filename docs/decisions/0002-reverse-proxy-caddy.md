# ADR 0002 — Caddy reverse proxy for the standalone stack

- **Status:** Accepted (2026-06-29)
- **Related:** [0001-auth-and-multi-user.md](0001-auth-and-multi-user.md), [../deployment.md](../deployment.md)

## Context

With the app, Postgres, and Logto all in Docker Compose, the **containerized** app could not
complete OIDC: the browser reached Logto at `http://localhost:3001`, but inside the `calendar`
container `localhost` is the container itself, so the server-side back-channel (PAR / token /
JWKS / Management API) couldn't reach Logto. The OIDC issuer must be a single URL that resolves
identically for the browser *and* the app. As a stopgap we ran the app on the host; this ADR
replaces that with the reverse proxy the architecture already called for ("one reverse proxy").

## Decision

Add a **Caddy** service (standalone profile) as the single entry point on port **8080**, routing
by host:

| Host | Upstream |
|---|---|
| `localhost:8080` | calendar app |
| `auth.calendar.localhost:8080` | Logto OIDC + Management API (`logto:3001`) |
| `admin.calendar.localhost:8080` | Logto admin console (`logto:3002`) |

- **Browsers** resolve `*.localhost` → `127.0.0.1`, so only port 8080 needs forwarding.
- **Containers** resolve the auth/admin hostnames via Docker **network aliases** on the Caddy
  service, so the app's back-channel uses the *same* URL the browser does.
- Logto's `ENDPOINT`/`ADMIN_ENDPOINT` and the app's `LOGTO__ISSUER`/`LOGTO__MANAGEMENT__ENDPOINT`
  point at the Caddy hostnames. The app stays on bare `localhost:8080`, so the **existing Logto
  redirect URIs (`/signin-oidc`, `/signout-callback-oidc`) are unchanged**.

## Why

- It's the only approach that gives one issuer URL working from both sides without a per-host
  hack; it matches the ecosystem's "one reverse proxy" architecture and the eventual prod shape
  (TLS terminates at the shared proxy). Running the app on the host worked but diverged from how
  it actually ships.
- Keeping the app on `localhost` avoided re-registering Logto redirect URIs.

## Consequences

- The login round-trip is now **cross-site** (`localhost` ↔ `auth.calendar.localhost`), so the
  OIDC correlation/nonce cookies are set `SameSite=Lax` (they survive the top-level GET callback
  and stay sendable over plain http in dev). See [AuthenticationExtensions.cs](../../src/CalendarApi/Auth/AuthenticationExtensions.cs).
- `caddy` and `logto` no longer publish ports directly; only Caddy publishes 8080. Forward just
  that one port.
- Production swaps `auto_https off` for real TLS at the proxy; the routing model is the same.
- **Data Protection keys are persisted in Postgres** (`PersistKeysToDbContext`, table
  `DataProtectionKeys`) so auth/antiforgery cookies survive container recreates — otherwise every
  rebuild rotated the ephemeral keys and invalidated existing cookies ("key not found" / OIDC
  "Correlation failed"). Note: cookies issued *before* this change can't be decrypted; clear them
  once (or use a fresh browser session).
