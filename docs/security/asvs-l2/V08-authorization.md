# V8 — Authorization

**ASVS 5.0 L2** · [← dashboard](README.md) · the core chapter for Calendar's access model.

## Status summary

✅ Implemented and tested ([ElementEndpointsTests](../../../tests/CalendarApi.IntegrationTests/ElementEndpointsTests.cs)).

---

## V8.1 — Authorization documentation

| Req | L | State | Notes |
|-----|---|-------|-------|
| V8.1.1 | 1 | ✅ | Function- and data-level rules documented in [../../auth.md](../../auth.md) §Access model + §Authorization. |
| V8.1.2 | 2 | ➖ | No field-level access differentiation — owner sees/edits all fields; assignees read all fields. Reassess if roles land. |

## V8.2 — General authorization design

| Req | L | State | Notes |
|-----|---|-------|-------|
| V8.2.1 | 1 | ✅ | `RequireAuthorization()` on every `/api/*` group ([ElementEndpoints.cs](../../../src/CalendarApi/Endpoints/ElementEndpoints.cs)) + the cookie/JWT default policy in [Program.cs](../../../src/CalendarApi/Program.cs). Anonymous → 401 (`List_Anonymous_Returns401`). |
| V8.2.2 | 1 | ✅ | EF Core global query filter on `Project`/`WorkTask` keyed off `ICurrentUser` ([CalendarDbContext.cs](../../../src/CalendarApi/Data/CalendarDbContext.cs)) — cross-user rows invisible; IDOR/BOLA blocked at the data layer (`List_OnlyReturnsOwnOrAssigned`, `GetById_AsNonAssignee_Returns404`). |
| V8.2.3 | 2 | ➖ | No field-level (BOPLA) differentiation — same rationale as V8.1.2. |

## V8.3 — Operation-level authorization

| Req | L | State | Notes |
|-----|---|-------|-------|
| V8.3.1 | 1 | ✅ | Enforced at trusted layers only: endpoint `RequireAuthorization()` + the owner check in the store + the DB query filter. No client-side authorization. Owner-only edit/delete/assign; visible-but-not-owner → 403, not-visible → 404 (`AddAssignee_AsOwner_MakesVisible_ButAssigneeCannotEditOrDelete`, `Update_AsNonVisibleUser_Returns404`). |

## V8.4 — Other considerations

| Req | L | State | Notes |
|-----|---|-------|-------|
| V8.4.1 | 2 | ✅ | Cross-user isolation: an unset `ICurrentUser` matches no row (fail closed). Bypasses (`IgnoreQueryFilters()`) limited to seeding with a `// reason:` ([SampleData](../../../src/CalendarApi/Services/SampleData.cs)). Pinned by the cross-user forgery tests (see [../../testing.md](../../testing.md)). |
