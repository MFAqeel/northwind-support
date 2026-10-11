# Implementation approach

## Task 1 — Defect and filtering
The status filter did not refresh the ticket query. The UI now reloads on filter changes and the backend applies filtering before counting and paging. Separate commits preserve the original defect fix and filtering work.

## Task 2 — Triage and escalation
The domain owns ticket state transitions. Application handlers coordinate customer tier, category constraints, SLA calculation, eligible-agent selection and immutable escalation records. Read queries project the SLA status without loading entire graphs. SLA windows and at-risk percentage are configurable. Database indexes support history and ticket searching.

## Task 3 — Authentication
Agent credentials are stored alongside Agent because there is no separate user lifecycle. PBKDF2-HMAC-SHA256 (random salt, 210k iterations) provides salted password hashes; only a hash is stored. JWTs are validated by a fallback authorization policy, with only login and health public. Escalation actor comes from the validated identity, never from the body. Frontend stores the token in sessionStorage (tab-scoped but susceptible to XSS) and handles 401 centrally. No refresh tokens or roles were added.

## Task 4 — Docker
A multi-stage Dockerfile caches restore, publishes on the SDK image and runs under the ASP.NET runtime's unprivileged user on port 8080. Compose waits for healthy SQL Server, injects configuration at runtime, and exposes the API on localhost:5080. Auto-migration is a development-only convenience; production should run migrations as a controlled deployment step.

## Verification and limitations
Run `dotnet test` and `npm test`, then verify `docker compose up --build` with a clean volume and `/health`. Docker image sizes, cached-layer timings and integration results must be recorded after running on a machine with Docker/.NET. There is no refresh token, rate limiting, account lockout, OAuth, MFA or dedicated migration job. Review all AI-assisted changes before submission.
