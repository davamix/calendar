# ADR 0001 — Authentication, authorization & multi-user

- **Status:** Accepted (2026-06-29)
- **Supersedes:** the in-memory / single-user POC posture
- **Related:** [auth.md](../auth.md), [deployment.md](../deployment.md), [security/asvs-l2/](security/asvs-l2/)

> This is the first entry in Calendar's decision log. **Every big change is recorded here**
> (context → decision → why → consequences) so the *why* survives the change. Keep entries
> short; link to the living docs for the *how*.

## Context

Calendar shipped as a single-user POC: a .NET 10 minimal API + vanilla SPA with Postgres,
but **no authentication or authorization** — every endpoint anonymous, every row visible to
everyone. The next milestone is a real multi-user app. Calendar is also the **first
application of a planned ecosystem**, so its choices set the reference other apps will copy.
The ecosystem standards (vendored at `.standards/`) mandate a central identity provider
(Logto), JWT-validating apps, per-app database isolation, and a security baseline. We also
have a mature in-house reference, `davamix/acopio`, which already implements Logto + OWASP
ASVS L2 (knowledge source only — no shared runtime components).

## Decisions

1. **Identity provider: Logto.** Apps never store passwords or roll their own login.
   Sign-in/sign-up is delegated to **Logto's hosted page**; the app redirects there when
   there is no active session.
2. **Access model:** the creator of an element is its **owner**. Owner-only: edit, delete,
   add/remove assignees. **Assignees get a read-only view.** It is one shared element
   (owner edits propagate to all assignees). The creator is auto-added as an assignee so
   "visible to me = I own it or I'm assigned".
3. **Access gating (v1): any signed-in user** is a Calendar user (open sign-up). The
   assignee directory lists all Logto users via the Management API. **Logto Organizations**
   (per-app user scoping) is deferred until the second ecosystem app needs a boundary.
4. **Auth surface: dual-scheme.** A **BFF** (server-side OIDC code flow; tokens kept in an
   encrypted HttpOnly cookie) authenticates the **browser** — tokens never reach JavaScript.
   An **audience-scoped JWT-bearer** resource server authenticates **machine / inter-app**
   callers. `/api/*` accepts either.
5. **Keep the vanilla SPA — no Blazor.** The BFF gives the same token-safety (ASVS V10.1.1)
   without a UI rewrite.
6. **Security baseline: OWASP ASVS 5.0 L2**, tracked per-chapter under
   [security/asvs-l2/](security/asvs-l2/), mirroring Acopio's structure.
7. **Deployment: dual-mode.** The app is config-driven and runs **standalone** (bundles its
   own Logto + Postgres via a compose profile) or **integrated** (points at shared ecosystem
   infra). See [deployment.md](../deployment.md).
8. **Tooling:** a curated `.claude/agents/` reviewer set (security / access-control /
   architecture / migration / integration-test-author), adapted from Acopio.

## Why

- **BFF over a public SPA token:** a browser SPA holding access/refresh tokens is an XSS
  token-theft surface; ASVS L2 V10.1.1 steers explicitly to keeping tokens server-side.
  Blazor would deliver that "for free" but at the cost of a UI rewrite, a stateful SignalR
  circuit, and a contradiction of our lean-stack preference — so we get the BFF property
  without Blazor.
- **Dual-scheme now (not later):** `api-design.md` expects JWT-validating APIs for inter-app
  REST; building the bearer scheme alongside the cookie avoids a retrofit and lets us pin
  audience-isolation behaviour with tests from day one.
- **Owner-only mutation, read-only assignees:** the simplest model that satisfies the
  requirement and avoids an *unbounded ACL* (an earlier "any assignee can share" idea let
  anyone widen access without limit — rejected on least-privilege grounds).
- **Defer Organizations:** with one app, "any signed-in user" is sufficient and friction-free;
  the org boundary earns its complexity only when a second app must exclude users.
- **Logout lessons carried over (not the bugs):** Acopio's logout incidents (PRs #34, #67)
  were **Blazor-rooted** (SignalR-circuit `forceLoad`, an antiforgery token that lived only
  in the interactive circuit, a `UseStatusCodePagesWithReExecute` POST re-execution). Our
  vanilla SPA can't hit those, but the *general* rules still apply and are adopted up front:
  logout is an antiforgery POST → local sign-out + Logto `end_session` (registered
  post-logout redirect, open-redirect guard); the antiforgery token must reach the client at
  request time; any SPA-fallback re-execution is gated to GET/HEAD.

## Consequences

- A data-model migration adds `OwnerId` + assignee join tables; existing rows need a backfill
  owner. The store stamps owner/creator from the validated identity, never the request body.
- Authorization is enforced in two trusted layers: an EF Core **global query filter**
  (read isolation, blocks IDOR/BOLA) and an **owner check** in mutate handlers.
- The app gains a hard dependency on a reachable Logto issuer (bundled in standalone mode).
- New cross-cutting concerns are now standard here: antiforgery on cookie mutations, a
  least-privilege Postgres role, per-subject rate limiting, and a test suite with
  cross-user forgery tests.
- Several of these decisions are **candidates to promote to `davamix/standards`** once proven
  in Calendar; that promotion is itself deferred (decide after implementation).
