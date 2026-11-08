# ADR-001: Domain Model Strategy

- **Status:** Proposed for review
- **Date:** 2026-11-08
- **Issue:** [OSH-62](https://github.com/oshango/kintraffic/issues/62)

## Context

KinTraffic has API models, DTOs, in-memory repositories, a SQL schema, and Angular models that describe overlapping operational concepts using different names, identifier types, status values, and relationship constraints. The SQL schema names a `Devices` table, while the API and UI use `Asset`; no C# Junction or Telemetry model exists. Several SQL foreign keys are nullable, while API scalar IDs appear required. There is no declared aggregate strategy or module dependency policy.

The platform is currently organized as one ASP.NET Core application with feature-specific controllers, services, and repositories. The README lists SQL Server/Azure SQL and SignalR as technologies, but the API project has no EF Core package or DbContext registration. Detailed evidence is recorded in the [domain model](../domain-model.md) and [gap analysis](../domain-gap-analysis.md).

## Decision

1. Keep the application a modular monolith organized around Operations, Identity, Commercial, and Inventory ownership. Do not introduce service decomposition or a new architectural framework in OSH-62.
2. Use Junction, Device, Fault, and WorkOrder as the canonical operational terms. Device is the domain term for the physical equipment currently called `Asset` by the API/UI. Existing contracts remain unchanged until a separately reviewed compatibility plan is approved.
3. Model Junction-to-Device, Device-to-Fault, Fault-to-WorkOrder, and Device-to-Telemetry as the target operational references. A required target relationship does not mean the existing nullable SQL column is already valid; resolve and validate existing data before enforcing it.
4. Keep separate lifecycle boundaries for Junction/device assignment, Device, Fault, WorkOrder, and individual Telemetry records. WorkOrder references Fault rather than owning its lifecycle; Telemetry records are not loaded as an unbounded Device child collection.
5. User and Role are business/domain concepts owned by the Identity module. Operations may reference a User for assignment and actor attribution. OSH-62 does not implement ASP.NET Core Identity types/managers, authentication, authorization, JWT, or refresh tokens; those are deferred to OSH-65/66/67.
6. Commercial and Inventory own their respective records. Do not infer Contract/Invoice constraints or Inventory/WorkOrder stock transactions solely from API scalar fields.
7. Preserve existing RoadSegment/TrafficState and TrafficSignal contracts as adjacent traffic-operational concepts. Their mapping to Junction and Device remains unresolved until supported by domain evidence.
8. Treat lifecycle states as canonical vocabulary only. Define legal transitions, time semantics, identifier types, deletion behavior, and retention before persistence constraints are introduced.

## Alternatives Considered

- **Keep `Asset` as the canonical domain term:** rejected because the SQL schema already uses `Devices`, and OSH-62 explicitly establishes Device as the canonical physical-infrastructure concept. API compatibility is still preserved pending a separate migration plan.
- **Make every observed identifier an enforced relationship immediately:** rejected because the repositories are in-memory, the SQL FK columns differ in nullability, and some identifiers have no SQL counterpart. Data validity and compatibility have not been established.
- **Move to microservices or add generic repository/domain frameworks:** rejected because current evidence shows one application and does not justify deployment or abstraction overhead.
- **Treat Inventory items, Contracts, or Users as related to every operational record:** rejected because those relationships are not represented in code or schema, apart from the specific scalar references documented in the model.

## Consequences

### Positive

- Product, API, database, and UI discussions gain a shared vocabulary.
- Ownership is explicit without requiring runtime changes in this issue.
- Future persistence and security work has a documented list of relationship and lifecycle decisions to validate.

### Costs and Risks

- Existing `Asset` names and duplicated `Fault.JunctionId` remain until compatibility and data migration are addressed.
- The target required relationships conflict with nullable foreign keys and sample/API shapes; migrations must not simply enforce them without checking existing data and use cases.
- Status, ID, and timestamp inconsistencies can continue to produce contract ambiguity until follow-on work aligns them.
- Some target rules, especially junction-level faults and inventory consumption, require domain-owner confirmation.

## Reasons

The decision follows inspected evidence rather than introducing a new runtime architecture. It defines only the minimum ownership and reference rules needed for persistence, security, audit, and telemetry planning while retaining current public behavior. Nullable and unenforced relationships are explicitly called out instead of being presented as existing guarantees.
The relationship matrix in `domain-model.md` is the authoritative relationship reference for follow-on persistence work. Its Status column distinguishes repository-confirmed relationships from proposed constraints and unresolved business rules. `domain-model.mmd` is the editable Mermaid source; the diagram Markdown embeds a copy for renderers.

## Future Considerations

- [OSH-63: EF Core persistence and migrations](https://github.com/oshango/kintraffic/issues/63) must align mappings and constraints with this model after resolving nullable relationships, identifiers, states, and existing data.
- [OSH-64: Azure SQL infrastructure](https://github.com/oshango/kintraffic/issues/64) owns infrastructure and secure connection configuration, not domain ownership decisions.
- [OSH-65: Entra ID authentication](https://github.com/oshango/kintraffic/issues/65) must establish the external identity mapping.
- [OSH-66: authorization policies](https://github.com/oshango/kintraffic/issues/66) must define policy meaning for the target role vocabulary.
- [OSH-67: Angular authentication](https://github.com/oshango/kintraffic/issues/67) must align UI identity handling with API security without treating route guards as authorization.
- [OSH-68: validation, ProblemDetails, and logging](https://github.com/oshango/kintraffic/issues/68) can enforce API boundary validation and traceable error behavior.
- [OSH-69: health and observability](https://github.com/oshango/kintraffic/issues/69) owns operational health and telemetry visibility.
- [OSH-70: production API hardening](https://github.com/oshango/kintraffic/issues/70) owns route and OpenAPI exposure policy.
- [OSH-71: deterministic pipeline](https://github.com/oshango/kintraffic/issues/71) owns immutable build/deployment artifact behavior.
- [OSH-72: security scanning](https://github.com/oshango/kintraffic/issues/72) owns automated code, dependency, container, and infrastructure scanning.
- [OSH-73: realtime architecture](https://github.com/oshango/kintraffic/issues/73) owns delivery, scale, and retention decisions for realtime telemetry.

No OSH-63 or later implementation is performed by this ADR.