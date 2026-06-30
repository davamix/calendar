# ADR 0003 — Cross-app integration (sibling apps on shared infra)

- **Status:** Accepted (2026-06-30)
- **Related:** [0001-auth-and-multi-user.md](0001-auth-and-multi-user.md),
  [0002-reverse-proxy-caddy.md](0002-reverse-proxy-caddy.md), [../deployment.md](../deployment.md),
  [../ecosystem-integration.md](../ecosystem-integration.md),
  [../../.standards/architecture.md](../../.standards/architecture.md),
  [../../.standards/security.md](../../.standards/security.md)

> Sets the pattern for how Calendar integrates with other ecosystem apps. First instance:
> **Kanban** (a separate project/board manager) mirrors its projects into Calendar. The portable
> how-to for bootstrapping that other app lives in
> [ecosystem-integration.md](../ecosystem-integration.md); this ADR records the *why*.

## Context

Calendar is the first app of a planned ecosystem; a second app — **Kanban** — is about to be
built in its own isolated repo/devcontainer. Kanban has its own domain (`project`/`task` are
distinct entities from Calendar's), but when a Kanban *project* is created its owner wants it to
appear on **their** Calendar, with the project's assignees mirrored. Kanban *tasks* are **not**
mirrored. This is the first time two ecosystem apps interact at runtime, so the shape set here is
the reference others copy.

The standards already constrain the solution: *one shared instance of each capability, logically
partitioned*; *centralized identity (Logto), apps validate JWTs*; *one database per app*;
*service-to-service via OAuth2 client-credentials, no static keys*; *identify the user by
`iss`+`sub`, never from a client-supplied field*.

A naive option — publish Calendar's image and have Kanban's own compose run a private copy of
Calendar — was rejected: it creates a second Calendar (+DB) per integrating app, the exact "one of
everything per app" anti-pattern the architecture forbids.

## Decisions

1. **Apps are siblings, never nested.** Each app keeps its own repo, Dockerfile, and `standalone`
   compose profile bundling its own infra. **No app bundles another.** The "run them together"
   composition (shared Logto + Postgres + proxy + every app image) lives in the **ecosystem /
   platform repo** that already owns the shared infra.
2. **Images via GHCR.** Each app publishes a versioned, digest-pinnable image to GitHub Container
   Registry; the integrated compose references those tags. (Chosen over Docker Hub to match the
   GitHub-centric CI / Dependabot setup.)
3. **Inter-app data flows over REST, through the callee's API — never its database.** Kanban
   writes Calendar projects via Calendar's HTTP API; it has no access to the `calendar` DB. One DB
   + least-privilege role per app is preserved.
4. **Acting as a user uses OAuth2 token exchange (RFC 8693, on-behalf-of).** To make a mirrored
   Calendar project owned by the *human* (so it shows on their Calendar), Kanban exchanges the
   user's token for one scoped to `aud=https://calendar.api` that still carries the user's `sub`.
   Calendar's existing rule (owner = token `sub`, never from payload) then makes the human the
   owner with **no Calendar code change**. Purely machine work with no user context uses
   **client-credentials** instead.
5. **East-west traffic is app→app over the shared internal network**; the reverse proxy stays the
   single entry for browser / north-south traffic.
6. **The shared Logto issuer host becomes ecosystem-neutral** (e.g. `auth.<domain>`, not
   `auth.calendar.localhost`) once Logto is shared; every app pins the same issuer and keeps its
   own API audience.

## Why

- **Siblings over nesting:** keeps "one shared instance, logically partitioned" intact; each
  integrating app adds its own audience / DB / role, not another copy of the callee.
- **Token exchange over a machine token + owner field:** a plain client-credentials token would
  make *Kanban* the owner, so the project would never appear on the user's Calendar; passing the
  owner as a request field would violate the "never from a client-supplied field" rule and force a
  Calendar change. Token exchange satisfies the requirement while leaving Calendar's identity
  derivation — and its code — untouched.
- **REST, not a shared table:** the apps' `project`/`task` entities are deliberately different
  domains; coupling them through Calendar's API (not its schema) keeps each side free to evolve and
  preserves DB isolation.

## Consequences

- **New Logto objects per integrating app:** its own API resource (audience), a BFF web client,
  and a **token-exchange-capable client granted the callee's API resource**. Kanban's full set is
  enumerated in [ecosystem-integration.md](../ecosystem-integration.md).
- **Calendar is unchanged for the Kanban mirror** — the exchanged token is an ordinary JWT for
  `https://calendar.api`, so the existing bearer scheme and owner/assignee endpoints handle it.
  *To verify on first integration:* that Logto's token-exchange grant is available and issues a
  `sub`-carrying access token for the target resource. If it is not, the fallback is a guarded,
  explicitly-trusted "create-on-behalf" path on Calendar — a deliberate, single-caller exception to
  the never-from-payload rule, recorded as its own ADR if taken.
- **The ecosystem / platform repo** gains the integrated compose and ownership of the shared
  Logto / Postgres / proxy; app repos reference GHCR images, not each other.
- A **portable integration brief** ([ecosystem-integration.md](../ecosystem-integration.md)) is
  added so a new app can be bootstrapped in an isolated devcontainer with full knowledge of how to
  interact and how `.standards/` works.
- **Candidate to promote to `davamix/standards`** once proven with Kanban (like ADR 0001's
  promotions, deferred until after implementation).
