# KinTraffic Cross-Cutting Engineering Requirements

These requirements apply to the Angular frontend, ASP.NET Core API, EF Core/SQL Server persistence, and modular monolith. **Mandatory** requirements apply to implementation and review. **Conditional** requirements apply only when the described capability exists. **Future considerations** are not implementation mandates until a concrete need is accepted. These requirements do not authorize speculative infrastructure, abstractions, or broad refactoring.

## Identity Decision Record

The owner-delegated OSH-65 targets and unresolved inputs are recorded in [ADR-004](architecture/adrs/ADR-004-first-party-jwt-and-refresh-sessions.md). That ADR remains Proposed pending approver/date placeholders. It records the first-party JWT deviation from ADR-002; the linked ADR-002 cross-reference does not replace or silently amend its OIDC direction.

## 1. Scalability, Concurrency, and Retention

- **Mandatory:** Correctness and security invariants must never depend on process- or instance-local state. Use SQL Server constraints, concurrency tokens, transactions, or atomic conditional operations for shared state as appropriate. This includes refresh-token redemption, uniqueness, lockout state, and account/session state.
- **Mandatory:** Concurrent operations must preserve the invariant they modify. Add parallel-operation tests for concurrency-sensitive behavior. SQL Server-specific constraints, migrations, and concurrency semantics must be tested on real SQL Server; SQLite is not equivalent evidence.
- **Mandatory:** Authentication and refresh-token concurrency conflicts must map to the defined generic authentication/session failure, not an unhandled HTTP 500. Ordinary business conflicts may use their documented conflict response and must not be confused with authentication failures.
- **Conditional:** Best-effort rate limiting and caching may use instance-local state when their loss/reset behavior, per-instance scope, and resulting limits are documented. Do not use such state as the sole authority for identity, uniqueness, redemption, or lockout correctness.
- **Conditional:** Background jobs must remain safe when multiple instances can execute them. Use idempotency, leases, database coordination, or an equivalent mechanism when required by the job's side effects.
- **Mandatory:** Every table with potentially unbounded growth must have an explicit retention or archival policy. Purging is not mandatory for every table: retain audit and other records where legal, security, or operational requirements demand retention. Purge operations must be safe to retry and coordinated when multiple workers may run them.
- **Future consideration:** Add replicas, caches, partitioning, or distributed coordination only when measured load or a documented availability requirement justifies them.

## 2. Idempotency

- **Conditional:** Provisioning, logout, and deployment operations should be repeatable when their semantics permit. Preserve refresh-token single-use: a lost refresh response may require reauthentication; retries must not weaken replay detection.
- **Conditional:** Externally initiated operations that are not naturally idempotent should accept an idempotency key when duplicate requests are a real client or delivery concern.
- **Mandatory when keys are used:** Persist the idempotency record and perform the associated side effect in the same database transaction. Enforce uniqueness over principal, operation, and key. Define key-retention scope and behavior when the same key is supplied with a different payload. A payload mismatch must not repeat the side effect.
- **Mandatory when keys are used:** Define duplicate behavior while the original request is in flight: return a documented conflict or the stored result. Do not leave in-flight duplicate behavior dependent on a race or instance-local state.
- **Security invariant:** Refresh-token redemption is single-use and conditional on unconsumed, unrevoked, unexpired state. Reuse detection revokes the token family; an idempotency mechanism must not turn a replay into successful redemption.

## 3. Platform Independence

- **Mandatory:** Domain and application logic must not directly depend on a cloud-provider service. Provider-specific implementations belong in infrastructure adapters and configuration.
- **Mandatory:** Local development must work without Azure.
- **Mandatory:** Introduce abstractions only for a real second implementation, a meaningful test seam, or a genuine architectural boundary. Avoid speculative portability layers.
- **Future consideration:** Additional cloud providers or hosting platforms are not portability requirements until a concrete second target is approved.

## 4. Asynchronous Processing

- **Mandatory:** Use asynchronous I/O end-to-end where supported and propagate `CancellationToken` through asynchronous operations.
- **Mandatory:** Do not block asynchronous execution or start fire-and-forget work from request paths.
- **Conditional:** Move long-running or retryable work to background processing only when its duration, reliability, or request-latency needs justify that boundary.
- **Mandatory when background work exists:** Design for retries, observability, idempotency, and duplicate delivery. Multiple workers must not violate business invariants.
- **Future consideration:** Do not introduce queues, brokers, or distributed infrastructure without a demonstrated requirement.

## 5. Reliability and Database Migrations

- **Mandatory:** Preserve transactional integrity and enforce durable invariants with database constraints or equivalent atomic database operations.
- **Conditional:** Use bounded retries through the appropriate framework mechanism, such as EF Core execution strategies for SQL Server, for operations that are safe to retry. Define command and outbound HTTP timeouts at their boundaries.
- **Mandatory:** Never automatically retry a non-idempotent operation unless its retry safety is established. Retry policy, attempt bounds, and failure handling must be explicit.
- **Mandatory:** Treat migration repeatability, recoverability, and backward compatibility as separate properties; success in one does not imply the others. Prefer expand-and-contract changes when application versions may overlap during deployment.
- **Production requirement:** Run production migrations as a controlled deployment step using a reviewed migration bundle or SQL script. Coordinate at the database/deployment level so concurrent deployers cannot run conflicting migrations. Production application replicas must not independently apply migrations at startup.
- **Temporary exception, OSH-44:** Until [OSH-44](https://github.com/oshango/kintraffic/issues/44) supplies the controlled migration mechanism, startup migration is permitted only for a deployment verified to run exactly one API replica. Local Compose may retain its documented startup migration behavior. Do not infer this exception is enforced by the application: current `Program.cs` calls `Database.MigrateAsync()` at startup and has no replica-count guard. The single-replica condition must be enforced operationally; production rollout is blocked unless that is verified. Remove the exception when OSH-44 is delivered.

## 6. Observability and Health Checks

- **Mandatory:** Use structured logs and correlation identifiers for request and background-operation diagnostics. Use UTC consistently. Inject `TimeProvider` where time-dependent behavior needs deterministic tests.
- **Mandatory:** Never log credentials, access tokens, refresh tokens, security stamps, signing material, cookies, or `Set-Cookie` values. Minimize personal and otherwise sensitive data in logs.
- **Mandatory:** Keep security audit events separate from operational diagnostics, with distinct access and retention decisions.
- **Mandatory for platform probes:** Liveness and readiness endpoints return status codes only: `200` or `503`; they expose no component details or exception data. Liveness reports process health and must not depend on transient external services. Readiness verifies required dependencies.
- **Conditional:** Detailed component diagnostics may be exposed only through a separate authenticated diagnostics endpoint or an appropriately restricted operational interface.
- **Current gap:** The existing `GET /api/Health` returns a JSON status body and is used by container probes. It does not satisfy the status-only rule and must not be described as compliant until changed and tested.

## 7. Maintainability

- **Mandatory:** Keep the application a modular monolith with clear module boundaries. Apply SOLID, DRY, KISS, and YAGNI pragmatically; avoid broad refactoring that does not protect a stated requirement.
- **Mandatory:** Avoid interfaces with only one implementation unless they represent a genuine boundary or meaningful test seam. Do not add dependencies or packages without approval.
- **Mandatory:** Test important business invariants, integration behavior, failure behavior, and concurrency. Keep tests aligned with the actual provider semantics being claimed.
- **Mandatory:** Run SQL Server-specific migration, constraint, and concurrency tests against real SQL Server. SQLite tests are useful for fast relational behavior but must not be represented as proof of SQL Server behavior.
- **Conditional:** Add module-boundary tests where a defined dependency rule or ownership boundary needs meaningful automated protection.
