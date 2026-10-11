# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. **Filters are accepted but not applied yet** (Task 1). |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |

## Task 2 additions (review before submission)

Ticket creation uses category flags for forced priority and specialization, and configured SLA windows with a premium multiplier and floor. Assignment selects the least-loaded active qualified agent below capacity, using agent ID to break ties. If no eligible agent exists, creation still succeeds and returns a triage explanation. Escalation records before/after priority, agent, deadline, actor and reason. The SLA start timestamp is persisted so that the at-risk threshold remains correct after an escalation resets the window. SLA status is computed, not stored, and list filtering is performed in SQL before count and pagination.

The `TicketEscalations` composite index on `(TicketId, EscalatedAtUtc)` supports history retrieval in newest-first order. The escalation history table uses a restricted ticket FK to prevent deleting audit records by cascading. The migration includes an initial SLA-start backfill for existing tickets with due dates.

**Verification required:** Run `dotnet test` and `npm run test --workspace apps/web`, apply the migration to a local SQL Server instance, and check ticket creation, escalation, and all SLA filter values. This uploaded-file editing environment did not have a .NET SDK or an available npm package install, so these changes have **not** been build-tested here. The pre-existing EF model snapshot should be regenerated/checked with `dotnet ef migrations` before further schema migrations. Manual assignment is separate from automatic triage; simultaneous ticket creation can race on capacity and reference numbering, so a production implementation would add concurrency safeguards.


## Candidate implementation — Tasks 1–4

Task 1: UI status filter refresh and SQL-side filtering. Task 2: category-aware priority, premium SLA windows, capacity-aware assignment, escalation history and SLA state in the UI. Task 3: agent login and JWT-protected API. Task 4: API Docker image and Compose service.

### Development login
Use **alex.turner@northwind-support.example** / **LocalDemo!2026** (development fixture only). Other active seeded agents use the same fixture password; the inactive Yuki account cannot log in. Existing development databases receive hashed credentials at startup after migration.

### Local run
Set `Jwt__Key` to a random 32+ byte key in your shell (or load `.env` into the environment); `.env.example` is a local-only illustration. Start SQL Server, then `dotnet run --project apps/api/src/SupportDesk.Presentation`, and `npm run web`. Sign in at `/login`. JWTs expire after two hours; they are kept in `sessionStorage` and attached as bearer tokens. A 401 clears the session and redirects to login. `sessionStorage` is tab-scoped, but a script injected through XSS could read it; production should add a CSP and security review. Passwords are PBKDF2-HMAC-SHA256 with random salts and 210,000 iterations.

### Docker run
Copy `.env.example` to `.env`, replace `JWT_KEY` with a unique 32+ byte value, and run `docker compose up --build`. API: `http://localhost:5080`; health: `http://localhost:5080/health`. Run the web app with `VITE_API_PROXY_TARGET=http://localhost:5080 npm run web` (PowerShell: `$env:VITE_API_PROXY_TARGET='http://localhost:5080'; npm run web`). The API runs as a non-root user, migrates and seeds only when explicitly enabled in Compose, and obtains database and JWT settings from environment variables. Production migrations should run separately under deployment control, not on every app startup. Verify `docker compose exec api whoami`, `docker compose down -v && docker compose up --build`, image sizes (`docker images`), and Docker restore-layer cache on your machine; these results have not been measured here.

### Known limitations and verification
No refresh tokens, password reset, roles, MFA, lockout, rate limiting or production secret management. Run `dotnet test`, `npm test`, `npm run build`, and exercise login, 401, escalation, filtering and `/health` against SQL Server before submission. AI-assisted implementation requires a manual code review; see `APPROACH.md`.
