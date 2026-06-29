# Testing guide

How to write and run Calendar's tests. The `integration-test-author` agent treats this as the
source of truth.

## Test projects

| Project | Kind | Use for |
|---|---|---|
| `tests/CalendarApi.UnitTests` | Unit (no host) | Pure logic — validators ([ElementRequest.Validate](../src/CalendarApi/Models/ElementRequests.cs)), `CurrentUser` claim resolution |
| `tests/CalendarApi.IntegrationTests` | HTTP integration | Endpoints, the access model, JWT validation, the directory |

## Running

```bash
dotnet test CalendarPoc.slnx          # everything
dotnet test tests/CalendarApi.UnitTests          # fast, no Docker
dotnet test tests/CalendarApi.IntegrationTests   # boots a Testcontainers Postgres
```

Integration tests need a Docker daemon (the devcontainer has docker-in-docker).

## Fixtures

| Fixture | Collection | What it gives you |
|---|---|---|
| [`ApiFactory`](../tests/CalendarApi.IntegrationTests/Fixtures/ApiFactory.cs) | `Api` | Real Postgres + `TestAuthHandler` (auth as the `X-Test-User` header), CSRF + Logto faked. Default for endpoint/authz tests. |
| [`JwtApiFactory`](../tests/CalendarApi.IntegrationTests/Fixtures/JwtApiFactory.cs) | `Jwt` | Real `JwtBearer` validated against a local test key; `MintJwt(aud, sub)`. For audience/issuer tests. |

- **`CreateClientAs("user-a")`** authenticates as that user (its value is the `sub`). No header → `401`.
- Each factory boots its own pgvector container in `InitializeAsync` and migrates on host start.

## Conventions

- Config goes through `builder.UseSetting("Key:Path", value)` — **never** `Environment.SetEnvironmentVariable`
  (process-wide; clobbers across parallel collections).
- Replace external clients (`ILogtoManagementClient`) with fakes via `ConfigureTestServices`
  ([FakeLogtoManagementClient](../tests/CalendarApi.IntegrationTests/Fakes/FakeLogtoManagementClient.cs)).
- One container is shared per collection, so tests **must not** depend on each other: use unique
  names (`Guid.NewGuid()`) and query by the returned id, not absolute counts.
- Assertions use **FluentAssertions** (`.Should()`), mocks use **NSubstitute**.
- Test names: `Method_Scenario_ExpectedResult`.

## Mandatory: cross-user forgery tests

Every feature touching owner/assignee data needs at least one isolation proof (ASVS V8). The
pattern (see [ElementEndpointsTests](../tests/CalendarApi.IntegrationTests/ElementEndpointsTests.cs)):

1. Create a resource as user A.
2. Act as user B.
3. Assert `404` (not visible) or `403` (visible-but-not-owner) as appropriate.

Current coverage: visibility filtering, owner-only update/delete, owner-only assign/unassign,
assignee read access, anonymous `401`, and wrong-audience JWT `401`.
