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
- **Persistence:** currently an in-memory store (POC). The next milestone is real
  persistence — follow the architecture guideline (one PostgreSQL, per-app database).
- **Project-specific specs and backlog** live in [docs/](docs/), not in `.standards/`.
