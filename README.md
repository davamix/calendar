# Calendar

A multi-user calendar app for managing **work projects** and **daily tasks**. It ships a web
UI **and** a REST API. Data is persisted in **PostgreSQL**, and users sign in via **Logto**.

> Stack: **.NET 10** (ASP.NET Core minimal APIs) + vanilla HTML/CSS/JS frontend + EF Core/PostgreSQL,
> packaged with **Docker**.

- **Auth & access model:** [docs/auth.md](docs/auth.md) — Logto (BFF cookie for the browser, JWT
  bearer for machines); each element has an **owner** (creator) and **read-only assignees**.
- **Security baseline:** OWASP ASVS 5.0 L2, tracked in [docs/security/asvs-l2/](docs/security/asvs-l2/).
- **Deployment:** standalone or integrated — [docs/deployment.md](docs/deployment.md).
- **Decisions:** recorded as ADRs in [docs/decisions/](docs/decisions/).
- **Design system:** [docs/STYLEGUIDE.md](docs/STYLEGUIDE.md) (tokens in [`wwwroot/tokens.css`](src/CalendarApi/wwwroot/tokens.css)).

---

## Concepts

Two element kinds share the same shape and are referred to together as a *task element*:

| Kind      | REST resource    | Meaning                          |
|-----------|------------------|----------------------------------|
| `Project` | `/api/projects`  | Longer-running work projects     |
| `Task`    | `/api/tasks`     | Day-to-day tasks                 |

Each element has: `id` (GUID), `name`, `description`, `startDate`, `endDate` (dates are
inclusive, `YYYY-MM-DD`), and an optional `color` (hex, e.g. `#4f46e5`).

---

## Run with Docker (standalone — bundles Postgres + Logto + Caddy)

```bash
cp .env.example .env          # fill in the passwords (see docs/auth.md)
docker compose --profile standalone up --build
```

This starts PostgreSQL, Logto, the app, and a **Caddy** reverse proxy (the single entry point on
port **8080** — forward only that port). On first run, complete the one-time
[Logto console checklist](docs/auth.md#logto-registration-manifest-console-checklist) at the admin
console **http://admin.calendar.localhost:8080**, paste the resulting IDs into `.env`, and re-run
`up -d`. Then open **http://localhost:8080** — with no session you're redirected to Logto's hosted
sign-in/sign-up page (`http://auth.calendar.localhost:8080`).

See [docs/deployment.md](docs/deployment.md) for how the proxy makes the OIDC flow work end-to-end
and for integrated (shared-infra) mode.

## Run tests

```bash
dotnet test CalendarPoc.slnx   # unit + integration (integration uses Testcontainers → needs Docker)
```

---

## Web UI

- **Month view** — a list of every day in the current month; each day shows the projects
  and tasks active on that date (multi-day items appear on every day in their range).
  - The **current day** shows each element as a full chip with its name.
  - **Past and future days** show each element as a compact shape filled with its
    `color`: a **square `P`** for projects and a **circle `T`** for tasks (hover for the
    name and dates).
- **Legend aside** — a side panel lists every project and task active in the visible
  month, each shown as its **shape + full chip**; click an entry to edit it.
- **Navigate** months with `‹` / `›` / **Today**.
- **Create** via **+ Project** / **+ Task**; set the element's `color` as a hex code in the
  form. **Edit or delete** by clicking any item.
- **Search** by name (substring) or by exact ID using the search box in the header.

---

## REST API

Base URL: `http://localhost:8080`. **Every `/api/*` endpoint requires authentication** — the
browser via the BFF cookie, machine callers via a `Bearer` JWT (audience = the Calendar API
resource). The same operations exist for `projects` and `tasks`; substitute `{kind}`.

Reads return only elements you own or are assigned to; **edit/delete/assign are owner-only**
(visible-but-not-owner → `403`, not-visible → `404`).

| Method   | Route                                   | Description                                  |
|----------|-----------------------------------------|----------------------------------------------|
| `GET`    | `/api/{kind}`                           | List visible; `?name=foo` filters by name    |
| `GET`    | `/api/{kind}/{id}`                      | Get one (404 if not visible)                 |
| `POST`   | `/api/{kind}`                           | Create (201; creator becomes owner+assignee) |
| `PUT`    | `/api/{kind}/{id}`                      | Update (owner only)                          |
| `DELETE` | `/api/{kind}/{id}`                      | Delete (owner only)                          |
| `GET`    | `/api/{kind}/{id}/assignees`            | List assignee ids                            |
| `POST`   | `/api/{kind}/{id}/assignees`            | Assign a user `{ "userId": "…" }` (owner)    |
| `DELETE` | `/api/{kind}/{id}/assignees/{userId}`   | Remove an assignee (owner)                   |
| `GET`    | `/api/users`                            | User directory for the assignee picker       |
| `GET`    | `/api/me`                               | The signed-in user                           |
| `GET`    | `/login`, `POST /logout`                | BFF sign-in / sign-out                       |
| `GET`    | `/health`, `/openapi/v1.json`           | Health probe / OpenAPI document              |

**Request body** (create/update):

```json
{
  "name": "Sprint planning",
  "description": "Plan the upcoming sprint",
  "startDate": "2026-06-22",
  "endDate": "2026-06-22",
  "color": "#16a34a"
}
```

`color` is optional. Validation returns **400** with an RFC 7807 problem document when
`name` is empty, `endDate` is before `startDate`, or `color` is not a valid hex code
(`#rgb` or `#rrggbb`).

### Examples

Machine callers send a `Bearer` JWT (audience = the Calendar API resource) from Logto:

```bash
TOKEN=...   # an access token for the Calendar API resource

# Create a task
curl -X POST http://localhost:8080/api/tasks \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Write docs","description":"Draft the API docs","startDate":"2026-06-22","endDate":"2026-06-23"}'

# Search projects by name
curl -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/projects?name=mobile"
```

---

## Project layout

```
.
├── Dockerfile                 # multi-stage build (SDK → aspnet runtime, non-root)
├── docker-compose.yml         # app + (standalone profile) bundled Postgres + Logto + Caddy
├── Caddyfile                  # reverse proxy: routes app + Logto behind one port
├── db/init/                   # least-privilege role + bundled Logto DB (fresh-volume init)
├── CalendarPoc.slnx
├── docs/                      # auth, deployment, decisions (ADRs), security/asvs-l2
├── .claude/agents/            # security / access-control / architecture / migration reviewers
├── src/CalendarApi/
│   ├── Program.cs             # app wiring, DI, endpoint mapping, OpenAPI
│   ├── Auth/                  # dual-scheme auth + antiforgery
│   ├── Models/                # CalendarItem, Project, WorkTask, AppUser, assignees
│   ├── Data/                  # EF Core DbContext (global query filter)
│   ├── Services/              # store, ICurrentUser, Logto Management client, seeder
│   ├── Endpoints/             # element CRUD + assignees, auth, users
│   ├── Migrations/            # EF Core migrations
│   └── wwwroot/               # index.html, app.js, styles.css (the web UI)
└── tests/                     # CalendarApi.UnitTests + CalendarApi.IntegrationTests
```

## Notes & current limitations

- Access gating is "any signed-in user" for now; per-app scoping via Logto **Organizations** is
  deferred until a second ecosystem app needs it.
- MCP + dynamic client registration are out of scope (a distinct audience is reserved for later).
- No pagination yet; the API surface is unversioned.
