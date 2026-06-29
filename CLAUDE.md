# Calendar — project instructions

This project is part of an application ecosystem and follows shared specifications vendored
at [.standards/](.standards/) (a git submodule → https://github.com/davamix/standards,
pinned per project). For how this convention works, see
[.standards/working-with-standards.md](.standards/working-with-standards.md).

If `.standards/` is empty, hydrate it: `git submodule update --init --remote .standards`.

## Shared standards (imported)

@.standards/architecture.md
@.standards/security.md
@.standards/api-design.md

## Project-specific

- **Stack:** ASP.NET Core minimal APIs (.NET 10) serving a vanilla HTML/CSS/JS SPA as
  static files from `wwwroot`. No Blazor, no Swagger UI, no Node/Tailwind toolchain — keep
  it lean.
- **Layout:** API in [src/CalendarApi/](src/CalendarApi/) (Endpoints / Models / Services);
  frontend in [src/CalendarApi/wwwroot/](src/CalendarApi/wwwroot/).
- **Design system:** follow [docs/STYLEGUIDE.md](docs/STYLEGUIDE.md); tokens in
  `src/CalendarApi/wwwroot/tokens.css`. Tasks render as circles; show dates, not times.
- **Persistence:** PostgreSQL via EF Core (one database per app). The app connects as a
  dedicated least-privilege role — see [docs/security/postgres-least-privilege.md](docs/security/postgres-least-privilege.md).
- **Auth:** Logto (central IdP); the browser uses a **BFF cookie**, machine callers use a
  **JWT-bearer** scheme. Access model = owner + read-only assignees. See [docs/auth.md](docs/auth.md).
- **Security baseline:** OWASP ASVS 5.0 L2, tracked in [docs/security/asvs-l2/](docs/security/asvs-l2/).
  Reviewer agents in [.claude/agents/](.claude/agents/) help close findings on each diff.
- **Deployment:** runs standalone (bundled infra) or integrated (shared ecosystem infra) —
  see [docs/deployment.md](docs/deployment.md).
- **Decision log:** every big change is recorded as an ADR in [docs/decisions/](docs/decisions/)
  (context → decision → why → consequences). Add one for the next big change.
- **Project-specific specs and backlog** live in [docs/](docs/), not in `.standards/`.
