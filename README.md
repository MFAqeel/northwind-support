# Northwind Support Desk

Northwind Support Desk is a small ticket-management application built with **.NET 10**, **React 19 / TypeScript**, and **SQL Server**. It lets a support team raise tickets, find and filter them, assign work to agents, and track tickets through resolution.

This repository started as an existing application rather than a greenfield project. My goal for the assignment was to improve the parts that were already there, add the requested features, and keep the code understandable for someone maintaining it later.

## What I worked on

### 1. Fixing the ticket filter

The first issue was that changing the status filter in the UI did not reliably refresh the ticket list. The selected filter changed, but the data-fetching logic was not responding to that change as it should.

I fixed the refresh behavior and added a regression test. I also moved the actual filtering into the backend query, so searching, filtering, counting, sorting, and pagination happen against the database rather than relying on the browser to filter an already-loaded page. This matters because the total count and page results need to describe the same filtered set of tickets.

I kept the defect fix and server-side filtering in separate commits to make the changes easier to review.

### 2. Ticket triage, assignment, and escalation

Creating a ticket now does more than save the request. The application uses the ticket category and customer tier to decide its initial priority, calculate an SLA deadline, and look for an appropriate agent.

- A category can force a ticket to **Critical** priority. Otherwise, the requested priority is used, with **Medium** as the default.
- SLA windows are configurable: **Low 72 hours**, **Medium 24 hours**, **High 8 hours**, and **Critical 4 hours** by default.
- Premium customers receive half the standard SLA window, subject to a one-hour minimum.
- Categories requiring a specialist are assigned only to qualified agents.
- Assignment considers active agents who are below capacity, prefers the agent with the fewest open tickets, and uses agent ID as a stable tie-breaker.
- If nobody is eligible, the ticket can still be created without an assigned agent.

The ticket response includes a triage explanation so the result is easier to understand instead of looking like a set of arbitrary system decisions.

Tickets can also be escalated one priority level at a time. Escalation recalculates the deadline, evaluates assignment, and stores a history record with the previous and new values, the reason, and the actor. Resolved or closed tickets cannot be escalated, and Critical is already the highest priority.

SLA status is calculated from the ticket's timestamps rather than stored as a value that could become stale. The UI can show **Within SLA**, **At Risk**, **Breached**, **Met**, or **Not Applicable**. The at-risk threshold is configurable, with 25% of the SLA window remaining as the default. SLA filtering is performed in the database before pagination.

### 3. Agent login and authentication

I added agent login and JWT-based authentication to protect the API. Agent passwords are stored as salted **PBKDF2-HMAC-SHA256** hashes rather than plaintext. The React application has a login screen, protected routes, and centralized handling for expired or rejected sessions.

The access token is held in `sessionStorage`, so it is scoped to the browser tab. This is a reasonable choice for this assessment, although it still needs the usual protection against cross-site scripting in a production application. The API takes the escalation actor from the authenticated identity instead of trusting a name sent by the client.

**Development-only login:**

| Field | Value |
| --- | --- |
| Email | `alex.turner@northwind-support.example` |
| Password | `LocalDemo!2026` |

These credentials are for the seeded demo environment only. They must not be reused in a real deployment.

### 4. Docker setup (optional)

I included a multi-stage Dockerfile for the API and a Compose configuration for the API and SQL Server. Configuration, including the database password and JWT signing key, is supplied at runtime rather than baked into the image. The API image is intended to run as a non-root user and exposes a health endpoint.

**Docker is included but was not successfully run and verified on my machine before submission.** I prioritized the required application work when the local Docker engine was unavailable.

## Running the project locally

### Prerequisites

- .NET SDK 10
- Node.js 20.19+ (Node.js 22 recommended)
- SQL Server 2022 or a compatible SQL Server instance
- Docker Desktop **only if** you want to start SQL Server through Compose or test the optional Docker setup

### 1. Install dependencies

From the repository root:

```powershell
npm install
```

### 2. Configure the database and JWT key

The development configuration is in `apps/api/src/SupportDesk.Presentation/appsettings.Development.json`. Check that the connection string points to your running SQL Server instance.

For local development, set a signing key of at least 32 bytes in the **same PowerShell terminal** used to run the API:

```powershell
$env:Jwt__Key = "REPLACE_WITH_YOUR_OWN_RANDOM_KEY_AT_LEAST_32_BYTES"
```

Do not commit a real key. `.env` files are excluded by `.gitignore`, and `.env.example` is provided as a configuration reference. Note that Docker Compose reads `.env` automatically, while `dotnet run` does **not** automatically import every value from that file into PowerShell.

If Docker Desktop is available, you can start the development SQL Server container with:

```powershell
npm run db:up
```

Otherwise, use your existing local SQL Server installation and configure the connection string accordingly.

### 3. Apply database migrations

```powershell
npm run db:migrate
```

The project includes migrations for the escalation and agent credential changes. In the development configuration, the API may also apply migrations and seed demo data on startup when initialization is enabled.

### 4. Start the backend

```powershell
npm run api
```

The development API is normally available at `http://localhost:5043` or `https://localhost:7043`, with Swagger at `https://localhost:7043/swagger`.

### 5. Start the frontend

Open a second terminal:

```powershell
npm run web
```

Visit **http://localhost:5173** and sign in with the development account above.

## Useful commands

| Command | Purpose |
| --- | --- |
| `dotnet test` | Run the backend unit and architecture tests |
| `npm run test --workspace apps/web` | Run frontend tests |
| `npm run build` | Build the API and frontend |
| `npm run lint` | Run frontend lint checks |
| `npm run typecheck` | Check frontend TypeScript types |
| `npm run db:migrate` | Apply EF Core migrations |
| `npm run db:up` / `npm run db:down` | Start / stop the development SQL Server container |

## API overview

Most application endpoints require a valid bearer token after login.

| Endpoint | Purpose |
| --- | --- |
| `POST /api/auth/login` | Sign in and obtain an access token |
| `GET /api/tickets` | Search, filter, sort, and page tickets, including SLA filters |
| `GET /api/tickets/{id}` | View ticket details and triage information |
| `POST /api/tickets` | Create a ticket with automatic triage |
| `POST /api/tickets/{id}/escalate` | Escalate an eligible ticket |
| `GET /api/tickets/{id}/escalations` | View escalation history |
| `PATCH /api/tickets/{id}/status` | Change ticket status |
| `PATCH /api/tickets/{id}/assignment` | Assign or unassign a ticket |
| `GET /api/customers`, `/api/agents`, `/api/categories` | Load reference data |
| `GET /health` | Health endpoint |

## Design decisions and trade-offs

**Keep the existing architecture.** I kept the separation between domain rules, application handlers, infrastructure, and API controllers instead of rewriting the project around a new framework. That reduced the risk of introducing unrelated changes.

**Calculate SLA state when reading.** A stored `AtRisk` or `Breached` flag would become outdated simply because time passes. Deriving it from timestamps keeps it accurate. The SLA start time is tracked so escalation can reset the window correctly.

**Let SQL Server handle filtering and agent selection.** Fetching every ticket or agent into memory would be simpler to write initially, but would not scale well and could produce incorrect page counts. Database-side filtering and ordering are a better fit here.

**Do not block ticket creation when agents are full.** An unassigned ticket is visible work that can be handled later; rejecting a customer's request because no agent is currently free would be worse.

**Use JWT rather than introducing a full identity platform.** The assignment calls for a focused agent login flow. A full identity provider, refresh-token service, and role system would add complexity beyond the requested scope.

**Keep escalation history separate.** Each escalation is an audit record, not just an update to the current ticket. This makes it possible to explain how a ticket reached its present priority and deadline.

## Testing and verification

During development, I ran the backend tests after correcting the controller compilation errors and synchronizing the EF Core migration snapshot. The reported result was **40 tests, with 40 passing**. The **frontend production build also succeeded**.

These results confirm compilation and the covered automated checks; they do **not** prove every feature works end-to-end. I did not complete a full manual walkthrough of login, escalation, and SLA filtering against a running database, and I did not verify the optional Docker setup. The frontend test suite should also be run separately if time permits.

For a fuller local verification:

```powershell
dotnet test
npm run test --workspace apps/web
npm run build
```

Then manually check that login works, unauthenticated requests return `401`, ticket creation returns triage information, escalation records history, and SLA filtering produces the expected results.

## Known limitations

- No self-service registration, password reset, refresh tokens, MFA, OAuth/SSO, or role-based authorization.
- No login rate limiting or account lockout. These would be important for an internet-facing deployment.
- Browser `sessionStorage` is accessible to injected JavaScript; a production rollout would need a thorough XSS and content-security-policy review.
- Concurrent ticket creation or assignment could race against agent-capacity checks. Stronger database-level concurrency controls would be needed at higher traffic volumes.
- Development seeding and automatic migrations are conveniences, not a production deployment strategy. Production migrations should be applied deliberately.
- The optional Docker configuration has not been verified end-to-end in this submission.

## How I used AI tools

I used AI assistance to help understand the existing code, work through implementation options, generate and review code changes, and troubleshoot build and migration errors. I also used it to improve the clarity of the documentation.

I treated the existing project structure, assignment requirements, compiler feedback, and test results as the sources of truth rather than assuming generated code was automatically correct. AI assistance was part of the development workflow, not a substitute for testing. Some manual and integration verification remains outstanding, as described above.

For a task-by-task summary of the approach, see [`APPROACH.md`](APPROACH.md).
