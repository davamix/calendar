# Authentication & authorization

How Calendar authenticates users, authorizes access to elements, and integrates with Logto.
Decisions and rationale live in [decisions/0001-auth-and-multi-user.md](decisions/0001-auth-and-multi-user.md);
this is the *how*. Security requirements are tracked in [security/asvs-l2/](security/asvs-l2/).

## Access model

The two element kinds (projects, tasks) share one model. An element has an **owner** (its
creator) and a set of **assignees**. The creator is auto-added as an assignee, so the read
rule is uniform.

| Action | Allowed for |
|---|---|
| Create | any authenticated user |
| See on calendar / search | owner + assignees |
| Edit / delete | **owner only** |
| Add / remove assignees | **owner only** |

Assignees have a **read-only** view of the *same* element — the owner's edits propagate to
everyone. Assignees cannot edit, share, or leave. Identity for "owner" / "assignee" is the
Logto `sub`, derived from the validated session — **never** from the request body.

## Identity (Logto)

- **Hosted sign-in/sign-up.** When there is no active session the app redirects to Logto's
  hosted page (see <https://docs.logto.io/end-user-flows/sign-up-and-sign-in>). Calendar has
  no custom login/registration UI. The sign-in method (username+password, passwordless, …) is
  a Logto *sign-in experience* console setting, not app code.
- **Users are mirrored locally.** A lightweight `users` table keyed by the Logto `sub`
  (`Id` = sub, `Email`, `DisplayName`) is upserted on first login and whenever a user is
  referenced as an assignee. Owner/assignee columns are FKs to it. Users are identified by
  `iss`+`sub`, which Logto never reassigns.
- **Assignee directory.** The assignee picker lists Logto users via the **Management API**
  (M2M client-credentials). v1 lists all users; a future Logto **Organization** will scope it.

## Authentication — dual scheme

`/api/*` accepts **either** of two schemes (default authorization policy lists both):

1. **Cookie (BFF)** — for the browser. `GET /login` issues an `OpenIdConnect` challenge
   (code flow, PKCE) to Logto; on callback the host creates an encrypted HttpOnly cookie and
   keeps the tokens server-side (`SaveTokens=true`). **Tokens never reach JavaScript**
   (ASVS V10.1.1). The OIDC request includes `resource={Logto:Audience}` (RFC 8707) so the
   access token is a JWT for the Calendar API resource.
2. **JWT bearer** — for machine / inter-app callers. Validates signature via the Logto JWKS
   (`Authority` pinned to the issuer, `RequireHttpsMetadata`), `aud` == `Logto:Audience`,
   issuer, and lifetime; `alg:none` rejected; `MapInboundClaims=false` so `sub` is read raw.

`ICurrentUser` (scoped) resolves `sub`/email/name from `HttpContext.User` regardless of the
scheme — the single place identity is derived.

### Logout

`/logout` is an **antiforgery-protected POST** (not a GET — avoids the CSRF / forced-logout
vector). It signs out the local cookie, then redirects to Logto's `end_session_endpoint` with
a **registered** `post_logout_redirect_uri`, behind an open-redirect guard. The antiforgery
token is delivered to the SPA (cookie) so the logout form/header can submit it. If an SPA
fallback ever re-executes a short-circuited response, it is gated to GET/HEAD so a failed POST
returns its real status instead of re-running the antiforgery pipeline into a 500.

## Authorization — two trusted layers (ASVS V8)

1. **EF Core global query filter** on `Project`/`WorkTask`, keyed off `ICurrentUser` injected
   into `CalendarDbContext`:
   `e.OwnerId == current || e.Assignees.Any(a => a.UserId == current)`.
   All reads (list, get-by-id, search) are isolated at the DB layer → IDOR/BOLA blocked. An
   unset current user matches no row (**fail closed**). Legitimate bypasses (seeding) use
   `IgnoreQueryFilters()` with a `// reason:` comment.
2. **Owner check** in mutate handlers (PUT/DELETE/assign): a *visible-but-not-owner* request
   → `403`; a *not-visible* one → `404` (the filter already hid it).

## Logto registration manifest (console checklist)

Provisioned once per environment in the Logto admin console (bundled in standalone mode).
Record the resulting IDs in `.env` — see [deployment.md](deployment.md) and `.env.example`.

1. **API Resource** for the REST API. Indicator == `LOGTO__AUDIENCE` (e.g. `https://calendar.api`).
   Enable `offline_access`.
2. **Application → Traditional Web App** for the BFF.
   - **Redirect URI:** `http://<host>/signin-oidc` (ASP.NET's `CallbackPath`).
   - **Post sign-out redirect URI:** `http://<host>/signout-callback-oidc` — ASP.NET's
     `SignedOutCallbackPath`, which is the value the app actually sends to Logto. (The app then
     lands the browser on `/`, which is local and need not be registered.) **Registering only
     `/` causes "post_logout_redirect_uri not registered" on logout.**
   - Copy `ClientId`/`ClientSecret` → `LOGTO__WEB__CLIENTID` / `LOGTO__WEB__CLIENTSECRET`.
3. **Application → Machine-to-Machine** for the user directory. **Roles → assign the built-in
   "Logto Management API access" role** — without it the token request is rejected
   (`invalid_target`) and the assignee directory is empty (names show as raw user ids). Copy
   creds → `LOGTO__MANAGEMENT__CLIENTID` / `…CLIENTSECRET`; set `LOGTO__MANAGEMENT__ENDPOINT`
   and `LOGTO__MANAGEMENT__RESOURCE=https://default.logto.app/api`.

**Gotchas (from the Acopio reference):**
- The M2M token request must ask for `scope=all`, or every Management API call returns
  `403 auth.forbidden`. This is in code, not config.
- Do **not** change `LOGTO__MANAGEMENT__RESOURCE` away from `https://default.logto.app/api`.
- Without the RFC 8707 `resource=` indicator, Logto issues an **opaque** token the API can't
  validate as a JWT.

## Environment contract

| Variable | Purpose |
|---|---|
| `LOGTO__ISSUER` | OIDC issuer URL (incl. `/oidc/`); `Authority` for both schemes |
| `LOGTO__AUDIENCE` | Calendar API resource indicator; validated `aud` |
| `LOGTO__WEB__CLIENTID` / `LOGTO__WEB__CLIENTSECRET` | BFF (Traditional Web App) client |
| `LOGTO__MANAGEMENT__ENDPOINT` / `…CLIENTID` / `…CLIENTSECRET` / `…RESOURCE` | M2M Management API client (assignee directory) |

Secrets come from the environment only — never source (see [.standards/security.md](../.standards/security.md)).
`.env.example` documents the names with no values.

## Enforcement points (files)

- Auth wiring, schemes, BFF endpoints, antiforgery, CORS, rate limiting — [Program.cs](../src/CalendarApi/Program.cs)
- Identity abstraction — `src/CalendarApi/Services/ICurrentUser.cs`
- Global query filter + model config — [CalendarDbContext.cs](../src/CalendarApi/Data/CalendarDbContext.cs)
- Owner checks + assignee endpoints — [ElementEndpoints.cs](../src/CalendarApi/Endpoints/ElementEndpoints.cs)
- Owner/creator stamping — [EfElementStore.cs](../src/CalendarApi/Services/EfElementStore.cs)
- User directory — `src/CalendarApi/Services/ILogtoManagementClient.cs`
