# Calendar POC

A proof-of-concept calendar app for managing **work projects** and **daily tasks**.
It ships a web UI **and** a REST API for external integrations. Data lives **in memory**
(seeded with sample data on every start) — there is no database.

> Stack: **.NET 10** (ASP.NET Core minimal APIs) + vanilla HTML/CSS/JS frontend, packaged with **Docker**.

---

## Concepts

Two element kinds share the same shape and are referred to together as a *task element*:

| Kind      | REST resource    | Meaning                          |
|-----------|------------------|----------------------------------|
| `Project` | `/api/projects`  | Longer-running work projects     |
| `Task`    | `/api/tasks`     | Day-to-day tasks                 |

Each element has: `id` (GUID), `name`, `description`, `startDate`, `endDate` (dates are
inclusive, `YYYY-MM-DD`).

---

## Run with Docker (recommended)

```bash
# Build + start
docker compose up --build

# …or with plain docker:
docker build -t calendar-poc .
docker run --rm -p 8080:8080 calendar-poc
```

Then open **http://localhost:8080**.

## Run locally (.NET 10 SDK)

```bash
dotnet run --project src/CalendarApi
```

The console prints the listening URL (e.g. `http://localhost:5xxx`).

---

## Web UI

- **Month view** — a list of every day in the current month; each day shows the projects
  and tasks active on that date (multi-day items appear on every day in their range).
- **Navigate** months with `‹` / `›` / **Today**.
- **Create** via **+ Project** / **+ Task**; **edit or delete** by clicking any item chip.
- **Search** by name (substring) or by exact ID using the search box in the header.

Projects are shown in indigo, tasks in teal.

---

## REST API

Base URL: `http://localhost:8080`. The same operations exist for `projects` and `tasks`;
substitute `{kind}` with `projects` or `tasks`.

| Method   | Route                      | Description                                  |
|----------|----------------------------|----------------------------------------------|
| `GET`    | `/api/{kind}`              | List all; `?name=foo` filters by name        |
| `GET`    | `/api/{kind}/{id}`         | Get one by ID (404 if missing)               |
| `POST`   | `/api/{kind}`              | Create (201 + created element)               |
| `PUT`    | `/api/{kind}/{id}`         | Update (200, or 404 if missing)              |
| `DELETE` | `/api/{kind}/{id}`         | Delete (204, or 404 if missing)              |
| `GET`    | `/health`                  | Health probe                                 |
| `GET`    | `/openapi/v1.json`         | OpenAPI document for the API                 |

**Request body** (create/update):

```json
{
  "name": "Sprint planning",
  "description": "Plan the upcoming sprint",
  "startDate": "2026-06-22",
  "endDate": "2026-06-22"
}
```

Validation returns **400** with an RFC 7807 problem document when `name` is empty or
`endDate` is before `startDate`.

### Examples

```bash
# Create a task
curl -X POST http://localhost:8080/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"name":"Write docs","description":"Draft the API docs","startDate":"2026-06-22","endDate":"2026-06-23"}'

# Search projects by name
curl "http://localhost:8080/api/projects?name=mobile"

# Delete by id
curl -X DELETE http://localhost:8080/api/tasks/<id>
```

---

## Project layout

```
.
├── Dockerfile                 # multi-stage build (SDK → aspnet runtime)
├── docker-compose.yml         # maps host 8080 → container 8080
├── CalendarPoc.slnx
└── src/CalendarApi/
    ├── Program.cs             # app wiring, DI, endpoint mapping, CORS, OpenAPI
    ├── Models/                # CalendarItem base, Project, WorkTask, request DTO
    ├── Services/              # generic in-memory store + sample data seeder
    ├── Endpoints/             # generic CRUD+search endpoint mapper (shared by both kinds)
    └── wwwroot/               # index.html, app.js, styles.css (the web UI)
```

## Notes & limitations (it's a POC)

- **Data is in memory** — everything resets to the seeded sample data when the app restarts.
- CORS is wide open (`AllowAnyOrigin`) so external apps can call the API freely.
- No auth, no pagination, no persistence — intentionally out of scope for this POC.
