# Business-logic validation & limits

Documents *what* Calendar validates and *which limits* it enforces, so the expectations are an
explicit reference rather than implied by reading the code. Supports ASVS L2 V2.1.2 (logical
consistency of combined data) and V2.1.3 (business-logic limits).

## How validation is structured

Input validation is a hard gate at the API edge. The single validator for the write surface is
[ElementRequest.Validate](../../src/CalendarApi/Models/ElementRequests.cs), reused by both
create and update so the two can never drift. Handlers assume input is already valid.

## Field & cross-field rules

| Field / invariant | Rule |
|---|---|
| `Name` | required, ≤ 200 chars |
| `Color` | optional, hex `#rgb` or `#rrggbb` |
| `EndDate ≥ StartDate` | cross-field check (the one real multi-field invariant) |
| owner / assignees | **never** accepted from the request body — derived from the session, set server-side |

Add a row here whenever a new validator or cross-field invariant is introduced.

## Business-logic limits

| Limit | Value | Where |
|---|---|---|
| Per-subject API rate cap | 100 / min (tunable) | `RateLimiting:*` in `Program.cs` |
| `Name` length | ≤ 200 chars | `ElementRequest.Validate` |
| `Description` length | ≤ 1000 chars | request/model |

Health probes and the OAuth/OIDC discovery/login endpoints are exempt from rate limiting so
monitoring and sign-in are not throttled. Keep limit values in sync with the source of truth
(the validator / `Program.cs`) cited above.
