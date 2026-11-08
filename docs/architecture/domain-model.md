# KinTraffic Domain Model

Status: OSH-62 architecture baseline, prepared for review.

This document defines the canonical language and target ownership boundaries for KinTraffic. It does not change application contracts, persistence, authentication, authorization, or infrastructure. See the [relationship diagram](./domain-model-diagram.md), [gap analysis](./domain-gap-analysis.md), and [ADR-001](./adrs/ADR-001-domain-model-strategy.md).

## Evidence and Decision Labels

- **FACT** means the behavior or shape is present in the inspected repository.
- **TARGET** means the canonical model selected by this OSH-62 design baseline.
- **ASSUMPTION** means a point that requires domain-owner confirmation before it becomes an enforced invariant.
- **RECOMMENDATION** means a change proposed for a later implementation issue.

The target model is deliberately separated from current storage and API contracts. A field named `*Id` is not treated as a database relationship unless a foreign key or equivalent behavior is present.

## Domain Overview

KinTraffic manages traffic infrastructure operations and maintenance. Its central operational vocabulary is **Junction**, **Device**, **Fault**, and **WorkOrder**. Telemetry records measurements produced by devices. Identity, commercial records, and inventory are supporting business modules.

**FACT:** The current API is an ASP.NET Core modular application organized into feature folders and controllers. `Program.cs` registers concrete in-memory repositories and services. The repository has no C# `Junction` or `Device` model; `Asset` is the C# and API term, while SQL calls the corresponding table `Devices`.

**TARGET:** Use `Device` as the canonical term for physical equipment installed at a junction. Keep existing `Asset` API contracts compatible until a separately reviewed change defines their migration. A junction owns the assignment of its devices; faults and work orders have their own operational lifecycles and refer to other entities by identifiers.

## Business Modules

| Module | Responsibility | Current repository evidence |
| --- | --- | --- |
| Operations | Junctions, Devices, Faults, Telemetry, WorkOrders, and traffic-operation views. Owns infrastructure identity and maintenance workflows. | Asset, Fault, and WorkOrder models and feature folders; TrafficSignal, RoadSegment, TrafficState APIs; SQL tables Junctions, Devices, Telemetry, Faults, WorkOrders, RoadSegments, TrafficStates. |
| Identity | Application user profile, role membership, and identity claims. Owns identity-to-application mapping; operational modules store user identifiers rather than identity credentials. | `UserProfile` has an ID, username, and string-array roles. `AuthController` currently returns a demo token. No role or claim model is present. |
| Commercial | Supplier/service Contracts and their commercial Invoices. Owns agreement and invoice lifecycle. | Contract and Invoice models, repositories, services, DTOs, and read endpoints. No corresponding tables are present in `infra/database/schema.sql`. |
| Inventory | Stock items and, when defined, parts and stock-control activity supporting maintenance. Owns stock quantity and item identification. | `InventoryItem` and its read API. No part-use, reservation, or stock-movement relationship to WorkOrders is present. |

These are logical ownership boundaries in one application, not separate deployables. The README lists related product modules, but the code does not yet enforce module-level dependency rules.

Audit is a cross-cutting capability rather than a fifth business module in this baseline. **FACT:** `AuditLog` has an in-memory repository/service and read endpoint, but no enforced reference to the actor or audited entity and no documented module ownership. **RECOMMENDATION:** Keep audit event creation with the module performing the business action; define a shared event contract and durable audit ownership when the relevant implementation work is approved.

## Entity Definitions

### Junction

- **Purpose (TARGET):** A traffic-controlled intersection and the operational location for installed devices.
- **Why it exists:** Operations need a stable location identity to group infrastructure and present its operational status.
- **Current evidence (FACT):** `infra/database/schema.sql` defines `Junctions(Id, Name, Location, CreatedAt)`. There is no C# Junction model, DTO, controller, repository, or Angular Junction model. The table has no operational-status column; `Name` and `Location` are nullable in SQL.
- **Relationships:** One Junction has many Devices. Device assignment is owned by Operations and represented by `Device.JunctionId` in the target model.
- **Lifecycle:** Junction creation, retirement, and operational-status transitions are not implemented or specified in the current repository.

### Device

- **Purpose (TARGET):** A physical asset installed at a Junction, such as a controller, traffic light, detector, or camera.
- **Why it exists:** It is the maintainable and observable equipment unit that produces measurements and may experience faults.
- **Current evidence (FACT):** The API has an `Asset` model/DTO with `Id`, `JunctionId`, `Name`, `Type`, and `Status`. SQL has a `Devices` table with `JunctionId`, `SerialNumber`, `Type`, `Status`, and `InstalledAt`; SQL has no corresponding `Name` field, while the API has no SerialNumber or InstalledAt field. The SQL Junction foreign key is nullable. Angular narrows `Asset.Type` and `Asset.Status` to string unions; the API exposes free-form strings with defaults of `controller` and `online`, so the API does not enforce the same unions.
- **Relationships:** Belongs to one Junction in the target model; may be the source of many Faults and Telemetry records.
- **Lifecycle:** Installation, maintenance, and retirement transitions are not defined consistently. Device identity terminology and status vocabulary need alignment.

### Fault

- **Purpose (TARGET):** An operational issue affecting a Device and requiring diagnosis or remediation.
- **Why it exists:** It records the issue lifecycle independently from the device and any maintenance work used to resolve it.
- **Current evidence (FACT):** The API `Fault` model and DTO contain both `JunctionId` and `AssetId`, plus severity, status, description, and creation time. SQL `Faults` has `DeviceId`, `FaultType`, `Severity`, `DetectedAt`, and nullable `ResolvedAt`; its `DeviceId` foreign key is nullable. The schema has no `JunctionId`, `Description`, or `Status` column.
- **Relationships:** Belongs to one Device in the target model and may be associated with zero or more WorkOrders. A junction-level fault without a Device is not established by the target relationship supplied for OSH-62; confirm whether it is required before modeling it.
- **Lifecycle:** Canonical states are Open, Assigned, In Progress, Resolved, and Closed. Current API/UI states omit Closed; SQL stores a `ResolvedAt` timestamp rather than a status.

### WorkOrder

- **Purpose (TARGET):** A maintenance activity used to investigate or resolve a Fault.
- **Why it exists:** It tracks planned and executed work separately from the fault that originated it.
- **Current evidence (FACT):** The API model/DTO has `FaultId`, optional `TechnicianId`, `Status`, `Priority`, and `ScheduledFor`. SQL `WorkOrders` has nullable `FaultId`, `AssignedTo`, `Status`, `CreatedAt`, and nullable `CompletedAt`. These are not aligned field sets, and no SQL relationship to a user is defined.
- **Relationships:** Originates from one Fault and is assigned to a Technician represented by a User identity. The optional assignment is evidenced by nullable `TechnicianId`; its relationship is not enforced.
- **Lifecycle:** Canonical states are Created, Assigned, In Progress, Completed, and Cancelled. The current API/UI use Queued, Assigned, In Progress, and Completed. The mapping between Queued and Created is not assumed here.

### User

- **Purpose (TARGET):** A person or service participant recognized by KinTraffic business workflows. User is a domain identity reference, not an authentication credential or implementation type.
- **Responsibilities (TARGET):** Provide a stable actor identity for work assignment and business accountability; participate in the business roles approved for the platform.
- **Ownership (TARGET):** The Identity domain owns the User concept and its business profile. Operations owns references to a User when assigning work or attributing operational actions. This boundary does not select an identity library or persistence model.
- **Current evidence (FACT):** `UserProfile` and Angular `AppUser` expose an ID, username, and roles. `WorkOrder` exposes an optional `TechnicianId`; SQL `WorkOrders.AssignedTo` is a string. `AuditLog.Actor` is a string. These are not foreign-key relationships. `AuthController` returns a hard-coded demo token and lowercase `admin`; this is not production identity evidence.
- **Relationships:** A User may be assigned to WorkOrders as a Technician. A User may hold Roles; the current `Roles` string array suggests membership but does not define a Role entity, inverse membership, or relational cardinality.
- **Lifecycle expectation (PROPOSED):** A business User profile may be provisioned, active, suspended, and retired. Assignment records should retain a stable user reference when a profile is deactivated. This lifecycle is a domain recommendation only; status transitions and retention need owner approval.

### Role

- **Purpose (TARGET):** A named business responsibility classification that can be associated with Users.
- **Responsibilities (TARGET):** Identify the kind of operational or administrative responsibility a User holds. A Role is a domain concept and does not itself encode executable access rules.
- **Ownership (TARGET):** Identity owns the role vocabulary and User-to-Role membership. Each business module owns the description of work performed in its area; OSH-66 must decide authorization policies separately.
- **Current evidence (FACT):** C# `UserProfile.Roles` and Angular `AppUser.roles` are string arrays. There is no Role class, SQL table, claims model, or defined assignment lifecycle.
- **Target vocabulary (TARGET):** Administrator, Supervisor, Operator, Technician.
- **Responsibilities by role (PROPOSED; validate with domain owners):** Administrator supports platform stewardship; Supervisor coordinates operational work; Operator monitors/operates traffic infrastructure; Technician performs assigned maintenance. These descriptions do not define permissions.
- **Relationship:** User-to-Role membership is proposed as many-to-many; no relational cardinality is confirmed by current evidence.
- **Lifecycle expectation (PROPOSED):** Role definitions are governed and may be activated/deprecated; membership is granted and revoked with an effective business history where audit requirements demand it. Exact transitions and retention need owner approval.

### Identity Scope Boundary

OSH-62 defines User and Role only as business/domain concepts. It does **not** implement ASP.NET Core Identity, `ApplicationUser`, `IdentityRole`, `UserManager`, `RoleManager`, authentication, authorization, JWT, or refresh tokens. Identity implementation and access policies are deferred to OSH-65, OSH-66, and OSH-67.

### Inventory Item

- **Purpose (TARGET):** A stock-keeping record for a physical item available to maintenance operations. The canonical distinction, if needed, is between a Part catalog definition and its stock balance at a location; OSH-62 does not assert that these must be separate persisted entities.
- **Why it exists:** Work planning may depend on knowing what parts or supplies are available.
- **Current evidence (FACT):** `InventoryItem` combines SKU, name, on-hand quantity, location, and reorder level. There are no separate Part entities, stock-movement records, or WorkOrder references.
- **Relationships:** Supports maintenance operations conceptually. No direct Inventory-to-WorkOrder relationship is established; do not infer reservation or consumption behavior.
- **Lifecycle:** Stock adjustments, reservations, and issue/return transitions are not specified by current code.

### Contract

- **Purpose (TARGET):** A supplier, maintenance, or service agreement.
- **Why it exists:** Commercial operations need to represent the terms and active period of external service arrangements.
- **Current evidence (FACT):** The API model has vendor name, service type, start/end dates, and status. It is served by an in-memory repository. No SQL table or supplier entity is defined.
- **Relationships:** The Invoice model carries `ContractId`, indicating an intended reference; there is no SQL foreign key or enforced relationship.
- **Lifecycle:** Current status is a string defaulting to `active`; the set of valid transitions is unspecified.

### Invoice

- **Purpose (TARGET):** A commercial billing record associated with a Contract.
- **Why it exists:** It records amounts and payment/issuance state for commercial operations.
- **Current evidence (FACT):** The API model/DTO has `ContractId`, amount, issue time, and status. The sample invoice references a sample contract. No SQL Invoice or Contract table is defined.
- **Relationships:** The API identifier implies a Contract reference, but cardinality and referential integrity are not enforced by the repository.
- **Lifecycle:** API defaults to `issued`; the Angular type additionally includes `draft` and `paid`. These vocabularies require alignment.

### Telemetry

- **Purpose (TARGET):** An operational measurement generated by a Device, such as cycle time, queue length, detector data, or another defined metric.
- **Why it exists:** It provides time-stamped evidence for operational monitoring and future analytics.
- **Current evidence (FACT):** SQL defines `Telemetry(Id, DeviceId, Metric, Value, RecordedAt)` with nullable `DeviceId` and nullable measurement fields. No C# entity, DTO, repository, service, controller, or Angular telemetry model exists. `TrafficState` is a separate road-segment measurement model.
- **Relationships:** Generated by one Device in the target model. SQL currently permits a telemetry row without a Device.
- **Lifecycle:** Retention, units, validation, ingestion, and realtime delivery are unspecified. These are architecture decisions, not assumed behavior.

### Adjacent Traffic and Audit Records

- **RoadSegment / TrafficState (FACT):** RoadSegment represents a named linear road segment and geometry. TrafficState records congestion, average speed, vehicle count, and timestamp for a RoadSegment. SQL enforces a required RoadSegment foreign key. These records are distinct from Junction/Device telemetry in the current model.
- **TrafficSignal (FACT):** The API/UI expose a signal name, status, cycle time, queue length, and update time. It has no Junction or Device identifier and is held in an in-memory repository. Its identity relative to Device and Junction is unresolved.
- **AuditLog (FACT):** The model records an entity label, action, actor string, and occurrence time. It is in-memory and has no enforced relationship to User or an audited entity.

## Relationship Matrix

The matrix is the authoritative relationship reference for OSH-63 and later. **Source Entity** initiates the relationship; **Target Entity** is the related entity. Cardinality uses the requested notation. Required/Optional describes the target domain rule; current nullability is stated in Evidence. Ownership names the domain/aggregate responsible for the relationship, not a deployment boundary.

Status applies to the target relationship as recorded here:

- **Confirmed:** The relationship and stated cardinality are verified by repository evidence. This does not imply stronger requiredness than the source schema/API enforces.
- **Proposed:** A target-model recommendation based on the OSH-62 domain objective; it is not yet enforced.
- **Unresolved:** Evidence suggests a relationship or concept but does not establish its target cardinality or business rule.

| Source Entity | Target Entity | Relationship Type | Cardinality | Required/Optional | Ownership | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Junction | Device | Owns placement | 1:N | Required for each Device in target; currently optional in SQL | Operations / Junction placement | [`schema.sql`](../../infra/database/schema.sql) `Devices.JunctionId` nullable FK; [`Asset.cs`](../../api/KinTraffic.Api/Models/Asset.cs) `Asset.JunctionId`; [`AssetDto.cs`](../../api/KinTraffic.Api/DTOs/Assets/AssetDto.cs) | Proposed |
| Device | Fault | May generate | 1:N | Required for each Fault in target; currently optional in SQL | Operations / Fault lifecycle | [`schema.sql`](../../infra/database/schema.sql) `Faults.DeviceId` nullable FK; [`Fault.cs`](../../api/KinTraffic.Api/Models/Fault.cs) `AssetId` and `JunctionId`; [`FaultDto.cs`](../../api/KinTraffic.Api/DTOs/Faults/FaultDto.cs) | Proposed |
| Fault | WorkOrder | May generate | 1:N | Optional from Fault; each target WorkOrder requires one Fault; SQL currently allows null | Operations / WorkOrder lifecycle | [`schema.sql`](../../infra/database/schema.sql) nullable `WorkOrders.FaultId`; [`WorkOrder.cs`](../../api/KinTraffic.Api/Models/WorkOrder.cs) `FaultId`; [`WorkOrderDto.cs`](../../api/KinTraffic.Api/DTOs/WorkOrders/WorkOrderDto.cs) | Proposed |
| Device | Telemetry | Generates | 1:N | Each target measurement requires one Device; SQL currently allows null | Operations / Telemetry record | [`schema.sql`](../../infra/database/schema.sql) nullable `Telemetry.DeviceId`; no C# or API Telemetry type is present | Proposed |
| RoadSegment | TrafficState | Has observations | 1:N | Required for each TrafficState | Traffic module / TrafficState record | [`schema.sql`](../../infra/database/schema.sql) `TrafficStates.RoadSegmentId INT NOT NULL` FK; [`TrafficState.cs`](../../api/KinTraffic.Api/Models/TrafficState.cs) `RoadSegmentId`; [`TrafficStateDto.cs`](../../api/KinTraffic.Api/DTOs/Traffic/TrafficStateDto.cs) | Confirmed |
| Invoice | Contract | References | N:1 | Required in current API shape; referential existence is not enforced | Commercial / Invoice | [`Invoice.cs`](../../api/KinTraffic.Api/Models/Invoice.cs) `ContractId`; [`InvoiceDto.cs`](../../api/KinTraffic.Api/DTOs/Billing/InvoiceDto.cs); [`BillingRepository.cs`](../../api/KinTraffic.Api/Repositories/Billing/BillingRepository.cs) sample; no SQL Contract/Invoice tables | Unresolved |
| WorkOrder | User (Technician) | Assigned To | N:1 | Optional (`TechnicianId` is nullable) | Identity owns User; Operations owns assignment | [`WorkOrder.cs`](../../api/KinTraffic.Api/Models/WorkOrder.cs) `TechnicianId`; [`WorkOrderDto.cs`](../../api/KinTraffic.Api/DTOs/WorkOrders/WorkOrderDto.cs); SQL `WorkOrders.AssignedTo` is unlinked text | Unresolved |
| User | Role | Membership | N:N | Unresolved | Identity | [`UserProfile.cs`](../../api/KinTraffic.Api/Models/UserProfile.cs) `Roles`; [`user.model.ts`](../../ui/kintraffic-ui/src/app/features/security/models/user.model.ts) `AppUser.roles`; no Role entity/table or inverse membership | Proposed |

No InventoryItem-to-WorkOrder or TrafficSignal-to-Junction/Device relationship is included: the inspected repository provides no relationship evidence, and the business rules remain undefined. Do not infer either relationship when designing OSH-63 persistence.

## Aggregate Boundaries

**FACT:** No aggregate roots, value objects, domain services, or transactional boundaries are declared in the current code.

**TARGET recommendations:**

| Aggregate/lifecycle boundary | Owns | References and constraints |
| --- | --- | --- |
| Junction | Junction identity/location and the assignment of Devices to that Junction. | Device maintenance, Fault, WorkOrder, and telemetry lifecycles remain independently managed; deletion/cascade behavior is not defined. |
| Device | Device identity, type, status, and installation lifecycle. | References one Junction; Fault and Telemetry records reference Device by ID. High-volume telemetry is not an in-memory child collection. |
| Fault | Fault description, severity, status, and resolution lifecycle. | References one Device; WorkOrders reference Fault. Whether multiple WorkOrders may be active for one Fault needs an operational rule. |
| WorkOrder | Work planning, assignment, priority, and completion/cancellation lifecycle. | References Fault and optionally an Identity user. The Fault is not owned by the WorkOrder. |
| Telemetry record | One immutable measurement and its timestamp/source/metric. | References Device; retention and correction policy remain open. |
| Commercial and Inventory records | Contract/Invoice and InventoryItem lifecycles within their owning modules. | Cross-module references use IDs; relationship and stock transaction rules need confirmation before persistence design. |

These are design boundaries for future implementation, not existing runtime aggregates. In particular, they do not prescribe EF Core navigation properties or cascade rules.

## Ownership Rules

1. Operations is authoritative for Junction, Device, Fault, WorkOrder, and telemetry-source relationships.
2. Junction owns device placement; a Device belongs to one Junction in the target model.
3. Fault owns its status and resolution lifecycle. It references Device rather than duplicating the Junction relationship when the Junction can be derived from Device.
4. WorkOrder owns its assignment and work lifecycle. It references Fault and, when assigned, a Technician User ID.
5. Identity owns authentication identity, roles, and claims. Operational records store identity references only; they do not own credentials or role definitions.
6. Commercial owns Contracts and Invoices. Inventory owns stock identity and quantity. No inventory consumption/reservation is implied until that workflow is defined.
7. Telemetry owns measurement facts. Devices are its source; telemetry retention and delivery are not device lifecycle responsibilities.

## Domain Constraints

### Target Invariants

- A Device has exactly one owning Junction.
- A Fault has exactly one Device under the supplied OSH-62 target relationship.
- A WorkOrder has exactly one originating Fault; Technician assignment is optional until assigned.
- A telemetry measurement has exactly one source Device.
- Fault and WorkOrder states use the canonical lifecycle lists above; allowed transitions must be defined before enforcement.
- A role name alone does not grant authorization; policy meaning is deferred to OSH-66.

### Current Gaps and Unresolved Rules

- SQL allows null `Devices.JunctionId`, `Faults.DeviceId`, `WorkOrders.FaultId`, and `Telemetry.DeviceId`; each has an FK declaration, but the relationship is optional in the current schema.
- API string IDs are not validated against related objects by the in-memory repositories.
- Identifier type and generation strategy differ: road/traffic SQL and C# IDs are integers, while several API models use strings. A cross-module canonical ID type has not been selected.
- Fault/WorkOrder state vocabularies and timestamps differ between API, UI, and SQL.
- Current SQL uses both `GETDATE()` and `SYSUTCDATETIME()` defaults; C# uses `DateTime` without a consistent UTC contract.
- Junction operational-status vocabulary, Device type/status vocabulary, Fault transition rules, and WorkOrder transition rules require owner confirmation.
- Unique constraints, deletion/retention behavior, telemetry units, and junction-level Fault handling are not established by the evidence reviewed.

Do not treat unresolved items as implementation permission. Resolve them before writing migrations or changing public API contracts.