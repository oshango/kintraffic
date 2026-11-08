# KinTraffic Domain Gap Analysis

This assessment records the repository state reviewed for OSH-62 and the work needed to align it to the [canonical domain model](./domain-model.md). It is documentation only; no persistence, API, identity, authorization, or infrastructure changes are included.

## Repository Assessment

### 1. Current Domain Model

**FACT:** The current API models Assets, Faults, WorkOrders, Users, InventoryItems, Contracts, Invoices, AuditLogs, TrafficSignals, RoadSegments, and TrafficStates. Junction and Telemetry appear in SQL but have no C# domain/API model. API repositories are in-memory. The current model is documented by feature DTOs and sample repository records rather than a canonical domain specification.

### 2. Current Entity Relationships

**FACT:** SQL declares nullable foreign keys for Device-to-Junction, Telemetry-to-Device, Fault-to-Device, and WorkOrder-to-Fault. RoadSegment-to-TrafficState is a required SQL foreign key. API-only references include `Fault.JunctionId`, `Fault.AssetId`, `WorkOrder.TechnicianId`, and `Invoice.ContractId`; these are not validated by the in-memory repositories. See the [relationship matrix](./domain-model.md#relationship-matrix) for requiredness, ownership, and evidence.

### 3. Current Module Boundaries

**FACT:** API folders separate Assets, Faults, WorkOrders, Traffic, Inventory, Contracts, Billing, and Audit into repositories/services; controllers expose corresponding read endpoints. `Program.cs` registers the concrete repositories and services in one ASP.NET Core application. Identity has a UserProfile and demo AuthController but no dedicated identity service/module. These folders are organizational boundaries, not enforced dependency boundaries.

### 4. Domain Inconsistencies

**FACT:** The physical asset is called Asset in API/UI and Device in SQL. API Asset has Name but no SerialNumber or InstalledAt, while SQL Devices has SerialNumber and InstalledAt but no Name. Fault carries both JunctionId and AssetId while SQL carries DeviceId. API/UI/SQL use different Fault and WorkOrder fields and state vocabularies. Angular narrows some status/type values that API models do not validate. User roles are free-form strings, including a demo lowercase `admin` value rather than the target role vocabulary.

### 5. Schema Inconsistencies

**FACT:** SQL keys for RoadSegments/TrafficStates are integers; API/C# identifiers for Assets, Faults, WorkOrders, Contracts, and Invoices are strings. Several SQL FKs that represent target ownership are nullable. Fault status is represented by `ResolvedAt` in SQL but by a string in API/UI. WorkOrder scheduling/assignment fields do not match SQL `CreatedAt`/`AssignedTo`. SQL time defaults mix local `GETDATE()` and UTC `SYSUTCDATETIME()`.

### 6. Missing Domain Concepts

**FACT:** No C# Junction, Device, Telemetry, separate Part, stock movement, Role, or Claims entity exists. The existing InventoryItem combines a stock item description with a location-specific on-hand quantity. There is no defined link from InventoryItem to WorkOrder or from TrafficSignal to Junction/Device. There are no documented aggregate roots or lifecycle transition rules. AuditLog exists, but its module ownership and actor/entity relationships are not defined.

### 7. Risks

**RECOMMENDATION:** Do not enforce the target required relationships until their business meaning and existing data are checked. Renaming Asset/Device or changing states can break API/UI consumers. The duplicated Fault identifiers can disagree. Missing telemetry units/retention and inventory consumption rules make premature schema design risky. See the detailed [risks](#risks) below.

### 8. Assumptions

The target Device term maps to the existing API Asset concept; target ownership is required even where SQL permits null; scalar fields `Invoice.ContractId` and `WorkOrder.TechnicianId` express intended references; and current statuses are not equivalent unless mapped. A Device-less junction-level Fault remains an unresolved business question. See [assumptions](#assumptions) below.

### 9. Recommendations

Confirm unresolved operational and commercial rules with domain owners before OSH-63 defines migrations; preserve API compatibility when aligning names and states; define identifier/time/deletion rules before storage constraints; and keep the modular monolith until concrete evidence supports a different deployment boundary. See [recommendations](#recommendations) below.

## Current State

### FACTS

- `infra/database/schema.sql` defines Junctions, Devices, Telemetry, Faults, WorkOrders, RoadSegments, and TrafficStates. It does not define Contracts or Invoices.
- The schema declares foreign keys from Devices to Junctions, Telemetry to Devices, Faults to Devices, WorkOrders to Faults, and TrafficStates to RoadSegments. The first four FK columns are nullable; `TrafficStates.RoadSegmentId` is required.
- C# models exist for Asset, Fault, WorkOrder, UserProfile, InventoryItem, Contract, Invoice, AuditLog, TrafficSignal, RoadSegment, and TrafficState. There are no C# Junction, Device, or Telemetry models.
- API and UI feature models use Asset/JunctionId/AssetId; SQL uses Devices/JunctionId/DeviceId. C# operational keys are strings while RoadSegment/TrafficState keys are integers.
- Fault has both `JunctionId` and `AssetId` in API/C#; SQL has `DeviceId`, `ResolvedAt`, and no JunctionId, Description, or status column.
- WorkOrder API/C# uses FaultId, optional TechnicianId, ScheduledFor, and statuses including `queued`; SQL uses nullable FaultId, AssignedTo, CreatedAt, and CompletedAt.
- Contract and Invoice repositories are in-memory. Invoice contains ContractId, but no SQL relationship is defined.
- UserProfile exposes roles as strings. `AuthController` returns a demo token and `admin`; there is no modeled claims relationship.
- Inventory has on-hand quantity and reorder level, but no parts catalog, stock movement, reservation, or WorkOrder link.
- Repositories use in-memory lists. `Program.cs` registers those concrete repositories; the API project file contains no EF Core package and no DbContext registration.
- Angular models mirror several API DTOs but differ in allowed values: Asset types/statuses, WorkOrder states, Fault states, TrafficSignal statuses, and Invoice states do not align as one contract with SQL/API.
- `README.md` lists product modules and target technologies, including SQL Server/Azure SQL and SignalR. It does not define canonical entities or relationships. The production runbook documents deployment operations, not the domain model.

### ASSUMPTIONS

- The OSH-62 target relationship makes Device the canonical name for the physical equipment represented by API Asset and SQL Devices. Existing API names are not renamed in this documentation task.
- A target Device must belong to exactly one Junction, and target Fault, WorkOrder, and Telemetry records must each have their stated owner/source. Existing nullable SQL columns may reflect incomplete data or an earlier design; their reason is unknown.
- `Invoice.ContractId` and `WorkOrder.TechnicianId` indicate intended references, but their cardinality, referential guarantees, and lifecycle rules are not proven by a database constraint.
- The Fault lifecycle requested in OSH-62 is the target vocabulary; current server, UI, and SQL representations are not assumed to be equivalent.
- A Device-less, junction-level Fault may be a valid business case because the API Fault includes JunctionId, but OSH-62's target relationship says Fault belongs to Device. Domain owners must resolve this before persistence constraints are designed.

## Target Domain

The target operational chain is Junction -> Device -> Fault -> WorkOrder. Device also generates Telemetry. Junction/Device placement is owned by Operations; Fault and WorkOrder have separate lifecycles; telemetry is a measurement record rather than a collection of unbounded state on Device.

Supporting modules are Identity (User and Role as domain concepts), Commercial (Contract, Invoice), and Inventory (stock items, parts, stock control). User/Role responsibilities and proposed lifecycles are documented without implementing identity or access-control technology. The repository does not establish Inventory-to-WorkOrder consumption or a formal Role entity. The [relationship matrix](./domain-model.md#relationship-matrix) is the authoritative relationship reference for OSH-63 onward. [ADR-001](./adrs/ADR-001-domain-model-strategy.md) records the architecture decisions and unresolved boundaries.

## Required Changes

1. Reconcile canonical Device with existing Asset API/UI contracts and SQL Devices without an unplanned breaking change.
2. Add and map Junction, Device, and Telemetry domain/persistence representations where absent; confirm which fields are authoritative.
3. Resolve the duplicate Fault JunctionId/AssetId representation and the junction-level-fault question.
4. Align required relationship semantics with actual nullable FK columns and existing data before enforcing constraints.
5. Reconcile C# and SQL field names, types, key types, and lifecycle timestamps for Fault and WorkOrder.
6. Define canonical status values and legal transitions, including the mapping or replacement for API/UI `queued` and the target Closed/Cancelled states.
7. Select consistent UTC timestamp semantics and a canonical identifier strategy.
8. Define the Identity user mapping for TechnicianId and the role/claims vocabulary before protecting APIs.
9. Decide whether Contracts/Invoices require durable SQL persistence and what referential constraints apply.
10. Define inventory part/stock movement and work-order consumption only if maintenance workflows require it.
11. Define Telemetry metric units, validation, retention, and ingestion separately from the realtime transport choice.
12. Confirm how TrafficSignal relates to Junction/Device and how it differs from RoadSegment/TrafficState.

## Deferred Changes

- EF Core, DbContext, repository refactoring, schema migrations, and persistence tests.
- Azure SQL provisioning, secret storage, and application connection configuration.
- Entra ID integration, authentication middleware, authorization policies, and production route protection.
- ASP.NET Core Identity, `ApplicationUser`, `IdentityRole`, `UserManager`, `RoleManager`, JWT, and refresh-token implementation.
- API validation/error middleware, health checks, Application Insights, and production OpenAPI policy.
- Pipeline, deployment, dependency, SAST, container, and Terraform scanning changes.
- SignalR/Azure SignalR selection, telemetry delivery, scaling, and retention implementation.
- Breaking API/model renames, Angular client behavior changes, and module extraction.

These belong to their approved issues and are not implemented by OSH-62.
These belong to their approved issues and are not implemented by OSH-62. User and Role remain domain vocabulary only; authentication is deferred to OSH-65, authorization policies to OSH-66, and Angular authentication to OSH-67.

## Future Issues Affected

| Issue | Domain-model dependency / expected follow-up |
| --- | --- |
| [OSH-63](https://github.com/oshango/kintraffic/issues/63) | Map entities and relationships; reconcile key types, nullable FKs, and status/timestamp fields before migrations. |
| [OSH-64](https://github.com/oshango/kintraffic/issues/64) | Provision Azure SQL and configure connections after target schema and migration ownership are defined. |
| [OSH-65](https://github.com/oshango/kintraffic/issues/65) | Map Entra identities to application User identifiers; remove demo-token assumptions. |
| [OSH-66](https://github.com/oshango/kintraffic/issues/66) | Define role/policy semantics for Administrator, Supervisor, Operator, and Technician; secure endpoints by policy. |
| [OSH-67](https://github.com/oshango/kintraffic/issues/67) | Align Angular session and route behavior with the API identity contract; frontend guards do not replace server authorization. |
| [OSH-68](https://github.com/oshango/kintraffic/issues/68) | Validate API DTOs against the canonical identifiers and lifecycle vocabularies; log domain failures safely. |
| [OSH-69](https://github.com/oshango/kintraffic/issues/69) | Expose dependency readiness and operational telemetry without conflating health probes with domain status. |
| [OSH-70](https://github.com/oshango/kintraffic/issues/70) | Review API routes and OpenAPI exposure against the agreed module and access boundaries. |
| [OSH-71](https://github.com/oshango/kintraffic/issues/71) | Promote the same tested artifact through environments; no domain changes are required by the model itself. |
| [OSH-72](https://github.com/oshango/kintraffic/issues/72) | Scan code, dependencies, containers, and Terraform; findings should be actionable for module owners. |
| [OSH-73](https://github.com/oshango/kintraffic/issues/73) | Decide telemetry contracts, delivery semantics, authorization, retention, and scaling after the Device/Telemetry relationship is confirmed. |

## Recommendations

1. Review the unresolved domain assumptions with Operations, maintenance, and commercial owners before OSH-63 defines migrations.
2. Treat the relationship diagram as an evidence map, not proof that all target constraints are already enforced.
3. Keep this canonical terminology stable across API, database, and Angular contracts, using explicit compatibility plans for any public rename.
4. Define status transitions, ID semantics, UTC rules, and deletion/retention policy before adding persistence constraints.
5. Keep the modular monolith and avoid cross-module navigation/cascade behavior until a concrete use case requires it.

## Risks

- Enforcing required relationships against current nullable columns may reject existing or valid data if the intended ownership rules are not confirmed.
- Renaming Asset to Device or changing state values may break clients if done without a versioned compatibility plan.
- The current dual Fault identifiers can disagree about which Junction owns the referenced asset.
- Demo identity and free-form role strings are unsuitable as evidence of production identity or authorization semantics.
- Missing telemetry retention/units and inventory consumption rules can lead to incompatible persistence and analytics choices if guessed in OSH-63.