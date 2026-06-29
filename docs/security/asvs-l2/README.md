# Calendar — OWASP ASVS 5.0 L2 verification

Per-chapter tracking of the OWASP ASVS 5.0 **Level 2** baseline for Calendar, mirroring the
structure proven in the `davamix/acopio` reference. **L2 is cumulative** (every L1 requirement
also applies at L2); L3-only requirements are out of scope.

> Source: [`OWASP/ASVS@v5.0.0`](https://github.com/OWASP/ASVS/tree/v5.0.0). This tracker is
> being populated as the auth/multi-user milestone lands (see
> [../../decisions/0001-auth-and-multi-user.md](../../decisions/0001-auth-and-multi-user.md)).

## Status legend

✅ Pass · ❌ Fail · ❓ Unknown · ➖ N/A · ⏳ Planned (target this milestone, not yet implemented)

**Evidence format:** `file:line`, config key, PR #, or a short note.

## Dashboard

| Chapter | Focus for Calendar | State |
|---|---|---|
| V1 Encoding & Sanitization | output encoding in the SPA (`escapeHtml`) | ⏳ triage |
| V2 Validation & Business Logic | [business-logic-validation.md](../business-logic-validation.md); rate limiting | ⏳ |
| V3 Web Frontend Security | CSRF/antiforgery, security headers, no open redirect | ⏳ |
| [V4 API & Web Service](V04-api-and-web-service.md) | JSON content types, no transparent HTTP→HTTPS on the API | ⏳ triage |
| V6 Authentication | delegated to Logto; passwords N/A if passwordless | ⏳ triage |
| [V7 Session Management](V07-session-management.md) | BFF cookie + Logto session lifetimes | ✅ implemented |
| [V8 Authorization](V08-authorization.md) | RequireAuthorization + global query filter + owner check | ✅ implemented + tested |
| [V9 Self-contained Tokens](V09-self-contained-tokens.md) | JWT validation hardening | ✅ implemented + tested |
| [V10 OAuth & OIDC](V10-oauth-and-oidc.md) | BFF token handling, dual-scheme, logout | ✅ implemented |
| V11 Cryptography | delegated to Data Protection key ring / Logto / TLS | ⏳ triage |
| V12 Secure Communication | TLS at the reverse proxy | ⏳ triage |
| [V13 Configuration](V13-configuration.md) | least-privilege DB role, secrets via env | ✅ implemented |
| V14 Data Protection | `Cache-Control: no-store` on API responses | ⏳ triage |
| V15 Secure Coding & Architecture | dependency scanning (CI), config-driven design | ⏳ triage |
| V16 Security Logging & Error Handling | RFC 9457 errors, security-event logging | ⏳ triage |

## Out of scope

- **V5 File Handling** — no user file uploads.
- **V17 WebRTC** — no peer-to-peer features.

## Process

1. Implement the control, then flip the row from ⏳ to ✅ with `file:line` / PR evidence.
2. Keep the chapter's status summary + this dashboard in sync.
3. The `security-reviewer` and `access-control-reviewer` agents (`.claude/agents/`) help close
   V7/V8/V9/V10/V13 rows on each diff.
