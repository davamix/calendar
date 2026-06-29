# Session & token model

Documents Calendar's session/token lifetimes and the coordination between the app (the Relying
Party / Resource Server) and Logto (the Identity Provider). Supports ASVS L2 V7 (session
management) and V10 (OAuth/OIDC). See [asvs-l2/V07-session-management.md](asvs-l2/V07-session-management.md)
and [asvs-l2/V10-oauth-and-oidc.md](asvs-l2/V10-oauth-and-oidc.md).

## Roles

- **Logto** is the Authorization Server / OpenID Provider — owns sign-in/sign-up and sessions.
- **Calendar** is both an **OIDC Relying Party** (the BFF cookie session for the browser) and
  an **OAuth Resource Server** (JWT-bearer for `/api/*` machine callers).

## Authentication pathway

A single pathway: Logto's **hosted sign-in/sign-up page**. There is no in-app login form and no
alternative sign-in path, so there is no cross-pathway strength inconsistency. The concrete
method (username+password, passwordless, social) is a Logto sign-in-experience console setting,
transparent to the app.

## Lifetimes

| Layer | Setting | Value | Where |
|---|---|---|---|
| **RP — BFF auth cookie** | Inactivity (sliding) | 12 h | `Program.cs` `ExpireTimeSpan` |
| | Absolute maximum | 7 d (re-auth regardless of activity) | stamped at sign-in |
| | Cookie flags | HttpOnly · Secure · SameSite=Lax · encrypted via the Data Protection key ring | |
| **IdP — Logto session** | Absolute lifetime | Logto config | Logto |
| **Access token** | TTL | ~1 h (Logto default on the API resource) | Logto |
| **Refresh token** | TTL · rotation | finite · rotation ON | Logto application settings |

These are **documented risk-based decisions** (V7.3): Calendar is a low-risk personal/team
scheduling tool and re-authentication via the hosted page is low-friction, so a tight-ish bound
is cheap. The RP absolute cap is aligned with (and bounded by) Logto's own session.

## Tokens stay server-side (V10.1.1)

The BFF holds access/refresh tokens inside the encrypted auth cookie (`SaveTokens=true`); no
token is ever delivered to browser JavaScript. Machine callers present their own JWT bearer.

## Logout & revocation

- **Logout** is an antiforgery POST → local cookie sign-out → redirect to Logto's
  `end_session_endpoint` with a registered `post_logout_redirect_uri` (open-redirect guarded).
- **Revocation** is owned by Logto (admin console; user self-service in the Logto account
  center). The app holds no independent session store to revoke.

## Step-up authentication

No Calendar operation currently requires elevated authentication strength or recentness, so
`acr`/`amr`/`auth_time` are not consumed. Revisit if a high-risk operation (e.g. a destructive
bulk action) is added.
