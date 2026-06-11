# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SGI (Sistema de Gestão Integrada) — full-stack enterprise system. Current version of these guidelines: **v2.1**.

---

## Tech Stack

### Frontend
- React.js + Vite, Material UI v5+, React Router DOM v6
- **MUI DataGrid Community (MIT)** — Pro/Premium features are out of scope without an approved ADR. All DataGrids use `paginationMode="server"` with `rowCount` from the API's `TotalCount`. Client-side pagination is forbidden.
- **Column filtering disabled on grids:** every DataGrid uses `disableColumnFilter`. All filtering happens via an external toolbar (server-side only).
- **TanStack Query is mandatory** for all server state: `useQuery`/`useMutation` only. Manual `useEffect + fetch` for server data is forbidden.
- Global client state (theme, session, UI): Context API or Zustand. Redux requires a formal ADR.
- Secrets via `import.meta.env.VITE_API_URL` — never hardcoded.

### Backend
- C# .NET 8 (LTS), Minimal APIs
- **Modularization is mandatory:** `Program.cs` stays declarative and lean (pipeline, DI, auth, rate limiting, CORS). Routes live in domain-specific extension methods (e.g., `Endpoints/ProdutoEndpoints.cs` with `app.MapProdutoEndpoints()`). Accumulating inline route handlers in `Program.cs` is forbidden.
- Each route group uses `MapGroup()` with a prefix, `RequireAuthorization()`, and shared metadata applied at group level (DRY).
- Secrets via `appsettings.json` + Secret Manager (DEV) / environment variables or secrets vault (PROD). Files with secrets never enter version control.

### Persistence
- Entity Framework Core v8, Code First + Migrations. All C# data access must be 100% database-agnostic.
- **DEV:** SQLite (`banco.db`) | **PROD:** PostgreSQL
- **DEV/PROD parity rules (critical):**
  - Raw SQL (`FromSqlRaw`/`ExecuteSqlRaw`) is forbidden without a formal ADR — use only LINQ translatable by the provider.
  - Provider-specific types (`jsonb`, `citext`, native UUID) are forbidden in the domain model; use portable C# types, isolate provider specifics in conditional DbContext configuration.
  - Text filter comparisons must be explicitly normalized (`ToLower()` on both sides) for identical behavior across both databases.
  - Integration tests must run against a real PostgreSQL instance via Testcontainers. Tests that only pass on SQLite do not validate PROD.
  - PROD deploy requires prior passage through a PostgreSQL staging environment.

---

## Architectural Rules

### Persistence & Queries
- **Soft Delete mandatory:** Hard delete is forbidden. Use a boolean `status` column (C# default `true`). Delete endpoints only set it to `false`.
- **Global Query Filter:** Soft delete is enforced via `HasQueryFilter` in DbContext (inactive records excluded by default). Repeated `Where()` per endpoint is forbidden (DRY). Access to inactive records only via `IgnoreQueryFilters()` on authorized admin routes.
- **Server-side pagination mandatory:** No list endpoint may call `ToListAsync()` without `Skip()` and `Take()`. Pagination and text filtering happen at the database level.
- **Typed API contract:** Always return `PagedResult<T> { Items, TotalCount, Page, PageSize }`, reused across all list endpoints.
- **Read-only queries:** Every read query uses `AsNoTracking()`.
- **Indexes mandatory for filters:** Every column exposed as a filter or sort in a search screen must have an index created in the migration delivered with the feature. Filter without index = full table scan in PROD = rejected in review.

### Search & Filtering
- **Closed filter list per screen:** Each search screen declares a fixed list of filterable fields at design time. Generic/dynamic filtering over arbitrary columns is forbidden.
- **Typed filter contract (backend):** Filters are named, strongly-typed parameters in the Minimal API handler signature (e.g., `string? nome, int? categoriaId, bool? ativo`). Generic filter objects, opaque JSON, or dynamic expressions are forbidden. Out-of-contract parameters are ignored; invalid values return 400 (Fail Fast).
- **Conditional IQueryable composition:** Filters applied via conditional `Where()` on `IQueryable` — an unfilled filter does not enter the query. EF must translate the entire composition into a single SQL. Materializing the collection and filtering in memory is forbidden.
- **Canonical query order:** `AsNoTracking()` → conditional `Where()` → deterministic `OrderBy()` → `CountAsync()` for total → `Skip()`/`Take()` → `ToListAsync()`.
- **Text filter:** Normalized with `ToLower()` on both sides.
- **Sensitive columns:** Columns with sensitive data (internal costs, margins, personal data) are not exposed to filtering without RBAC analysis; when needed, the filter is restricted to authorized roles.
- **Frontend filters:** External toolbar (outside the grid) with controlled inputs (`TextField`, `Select`, `DatePicker`), ~400ms debounce on text fields, filters modeled as a typed object composing the TanStack Query key (filter change = refetch + cache per combination).
- **Canonical search screen template:** Every new SGI listing is an instance of: `[typed filter toolbar + TanStack Query + server-side DataGrid + typed endpoint contract + PagedResult<T> + indexes]`. Any deviation requires an ADR.

### Security & Governance
- All generated code starts with JWT validating claims. Frontend sends the token via `Authorization` header.
- APIs enforce RBAC (`[Authorize(Roles = "Admin")]` or policies via `RequireAuthorization`). React routes protected via Route Guards.
- Production CORS must be restrictive. Implement global Rate Limiting and brute-force/DDoS protection using ASP.NET Core 8 native middlewares (`AddRateLimiter`, `AddCors`) — third-party dependencies for what the framework already solves are forbidden.

### Error Handling & Resilience
- **Backend (Fail Fast):** Hard cap on items per page (e.g., max 100). Validate inputs and return `400 Bad Request` early. Use `try/catch` around business logic to protect Kestrel. Return `Results.Problem()` for systemic failures — never expose stack traces. Expected status codes: 400, 401, 403, 404, 500.
- **Frontend:** All API consumption goes through TanStack Query with a centralized HTTP client/interceptor. Show user-friendly messages. Never expose stack traces, internal exceptions, or server details. 401 triggers re-authentication flow; 403 shows a permission-denied message.
- **Network resilience (frontend):** Automatic retry with backoff for transient failures (provided by TanStack Query). Explicit loading and empty-state handling in every listing.

### DTOs & Contracts
- Explicit DTOs for all API contracts. Exposing EF entities directly on routes is forbidden.

### Scalability Roadmap (YAGNI)
The items below are **not part of the MVP** and must not be implemented prematurely, but every design decision must avoid blocking them:
- Distributed cache (Redis) for hot reads and distributed rate limiting (multi-instance).
- Message broker for long-running async processes.
- Structured observability (`ILogger` structured logging from MVP; metrics and distributed tracing as evolution).

Any anticipation of these items requires a formal ADR.

### Decision Governance (ADRs)
- Every relevant architectural decision (library swap, rule exception, paid feature adoption, raw SQL usage) must be recorded as a versioned Architecture Decision Record in `docs/adr/`, with context, decision, and consequences.
- **ADR-001 (pending):** Adoption of MUI DataGrid Community with server-side filtering via external toolbar; comparative context of Community/Pro/Premium tiers and re-evaluation criteria.

---

## Response Protocol

Before generating any code for a new feature, **always** analyze and explain the impact across these 8 pillars:

1. Frontend
2. Backend
3. Persistence
4. Security (JWT & RBAC)
5. Pagination
6. Data Governance
7. Scalability
8. Application Resilience

Then generate production-ready code following Clean Code, SOLID, DRY, Separation of Concerns, Defensive Programming, Fail Fast, strong typing, and Secure by Default.

**When a request violates a rule in this document:** refuse the naive implementation, explain the risk, and propose the compliant alternative.

Never generate temporary solutions or simplified prototypes without explicitly flagging them as such.

---

## Changelog
- **v2.1:** Added `<search_and_filtering>` section: closed filter list per screen, typed filter contract in handler signature, conditional `Where()` on `IQueryable`, canonical query order, case normalization, RBAC governance for filterable fields, external toolbar with debounce and filters in TanStack Query key, canonical search screen template. Formal decision for DataGrid Community with `disableColumnFilter`. Mandatory indexes for filterable/sortable columns. ADR-001 previewed.
- **v2.0:** Mandatory Minimal API modularization; TanStack Query as mandatory server-state layer; DEV/PROD parity governance; MUI DataGrid licensing guidelines; global `HasQueryFilter` for soft delete; standardized `PagedResult<T>`; `AsNoTracking` on reads; page size hard cap; scalability roadmap under YAGNI; ADR governance instituted.
- **v1.0:** Original document.
