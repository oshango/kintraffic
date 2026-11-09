# Junction, Device, and Telemetry Read API Contracts

- **Status:** Proposed stable read contract; operational semantics and security approval remain open
- **Date:** 2026-11-09
- **Owner issue:** [OSH-75](https://github.com/oshango/kintraffic/issues/83)
- **Related decisions:** [ADR-002: KinTraffic-Managed Identity](./adrs/ADR-002-kintraffic-managed-identity.md), [ADR-003: Leaflet and OpenStreetMap Mapping](./adrs/ADR-003-leaflet-openstreetmap-mapping.md)

This document defines the confirmed data subset and proposed read routes for OSH-75. It does not claim the endpoints are implemented. Their exposure is gated on OSH-65/66 authentication and authorization decisions. Do not expose new operational data anonymously or change a public contract without a compatibility review.

## Repository Evidence

- `Junction` has a string ID, name, nullable free-form `Location`, and `CreatedAt`.
- `Device` has a string ID, required `JunctionId`, name, free-form type/status strings, nullable serial number, and nullable `InstalledAt`. EF Core enforces Device-to-Junction.
- `Telemetry` has an integer ID, required `DeviceId`, free-form metric, numeric value, and `RecordedAt`. EF Core enforces Telemetry-to-Device.
- No Junction, Device, or Telemetry controllers, DTOs, services, or repositories currently exist.
- Junction has no coordinates or operational status. Device has no last-seen timestamp or approved status vocabulary. Telemetry has no units and no direct Junction relationship.
- `Junction.Location` and `RoadSegment.Geometry` are strings; neither is a validated geographic coordinate/GeoJSON contract.
- The API uses ASP.NET Core controllers, camel-case JSON, built-in OpenAPI (`AddOpenApi`/`MapOpenApi`), EF Core, and SQL Server. Existing list endpoints return arrays; pagination is not yet a shared API convention.

## Shared Conventions

- Resource paths are plural and lowercase. Preserve existing routes while adding compatible aliases; do not silently rename or remove a route.
- String identifiers are opaque and case-preserving. Do not parse them as numbers or expose EF entities.
- List responses use a bounded page envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "totalCount": 0
}
```

- `page` is one-based and at least 1. `pageSize` defaults to 50 and is bounded to 1–100. Sort order is deterministic; endpoints document their sort keys.
- Timestamps in these contracts are ISO 8601 UTC with an explicit zero offset. `fromUtc` is inclusive and `toUtc` exclusive. Store/query conversion must preserve the existing UTC semantics of `SYSUTCDATETIME()`-backed `datetime2` fields.
- Invalid pagination or time ranges return 400 validation details; a missing resource returns 404; an existing resource with no associated records returns 200 with an empty page. OSH-68 owns consistent production ProblemDetails behavior.
- OpenAPI is generated from the actual controller metadata at `/openapi/v1.json`; do not maintain a conflicting hand-authored OpenAPI copy.
- Once OSH-65/66 are implemented, these operational reads require authentication and the approved Operations read permission. The exact policy name and any public exceptions are owned by OSH-66. Until then, do not release these new endpoints as anonymous production APIs.
- The API has no version prefix today. Keep these additive, unversioned routes for now; OSH-70 owns the versioning and compatibility policy.

## Junction

### `GET /api/junctions`

Query: `search` (optional name/location substring), `page` (default 1), `pageSize` (default 50, maximum 100). Sort by `name`, then `id`.

### `GET /api/junctions/{id}`

Returns 404 if the Junction does not exist.

### `JunctionResponse`

| Field | Type | Meaning |
| --- | --- | --- |
| `id` | string | Opaque persisted Junction identifier. |
| `name` | string | Persisted Junction name. |
| `location` | string or null | Existing descriptive location text; **not** coordinates and must not be passed to a map as a point. |
| `createdAt` | date-time UTC | Persisted creation timestamp. |

Do not return `status`, coordinates, or a monitored flag: no such authoritative Junction fields exist today. A Junction's associated Devices can be retrieved through the Device filter below; this avoids an unbounded nested response.

## Device

### `GET /api/devices`

Query: optional exact `junctionId`, `type`, and `status` filters; `page` (default 1); `pageSize` (default 50, maximum 100). Sort by `name`, then `id`. Filter values are the persisted strings; this contract does not define a closed enum or map status values to health colors.

### `GET /api/devices/{id}`

Returns 404 if the Device does not exist.

### `DeviceResponse`

| Field | Type | Meaning |
| --- | --- | --- |
| `id` | string | Opaque persisted Device identifier. |
| `junctionId` | string | Required ID of its persisted Junction. |
| `name` | string | Persisted Device name. |
| `type` | string | Persisted Device type; vocabulary is not yet constrained. |
| `status` | string | Persisted status value, returned verbatim; it is not interpreted as healthy/online without an approved vocabulary. |
| `serialNumber` | string or null | Persisted serial number, if present. |

Do not include `lastSeen`, health, coordinates, or `installedAt` in this initial response: last-seen/health fields do not exist, coordinates are unresolved, and `InstalledAt` has no documented timezone contract. Add them only after their semantics and migration compatibility are approved.

## Telemetry

### `GET /api/devices/{deviceId}/telemetry`

Query: optional `fromUtc` and `toUtc`; `page` (default 1); `pageSize` (default 50, maximum 100). The range must satisfy `fromUtc < toUtc`. Sort newest first by `recordedAt`, then descending `id`. A time range and bounded page size prevent unbounded history reads; no maximum time-span/freshness threshold is asserted here.

The route is Device-scoped because `Telemetry.DeviceId` is the confirmed relationship. Return 404 when the Device does not exist; return 200 with an empty page when it exists but has no matching Telemetry. There is no direct Junction telemetry route: querying by Junction would require aggregation over its related Devices and an approved use case.

### `TelemetryResponse`

| Field | Type | Meaning |
| --- | --- | --- |
| `id` | integer | Persisted Telemetry row identifier. |
| `deviceId` | string | Required source Device identifier. |
| `metric` | string | Persisted metric name, returned verbatim. |
| `value` | number | Persisted numeric value. |
| `recordedAt` | date-time UTC | Measurement event time, not an ingestion or last-seen time. |

Do not invent units or freshness classifications. A metric/units registry, permitted value ranges, maximum requested history span, and the threshold distinguishing current from stale data require approval. Missing Telemetry means “no telemetry received”; it does not mean healthy.

## Existing Asset/Fault Route Compatibility

The Angular application currently requests `/api/assets` and `/api/faults`; the API currently exposes `/api/Asset` and `/api/Fault`. Preserve the existing controller routes and add the plural lowercase paths as aliases to the same handlers/DTOs. Do not rename the legacy Asset contract to Device as part of this issue. API integration tests must prove both old and new paths return the same representation.

## Security and Implementation Gate

These are operational infrastructure reads. The controllers should be protected by the approved authentication/authorization policies before production exposure. Do not add a dummy scheme, trust Angular guards, or weaken security to make local tests pass. Local development may use a documented test identity only after OSH-65/66 define a production-isolated mechanism.

Implementation depends on OSH-65/66 for secure access and on the open decisions below for fields that cannot be inferred. The read-only contract subset using current fields may be implemented behind the security gate; coordinate, health, last-seen, unit, or freshness fields must remain absent until approved.

## OPEN — REQUIRES APPROVAL

- Authoritative Junction coordinate source, backfill, and API/storage point shape; see ADR-003.
- Whether `Junction.Location` is a human-readable description, legacy address, or another value; do not reinterpret existing rows.
- Junction/Device status vocabulary and the conditions for “registered”, “actively monitored”, “healthy”, “offline”, and “unknown”.
- Telemetry metric catalogue, units, valid ranges, timezone of legacy timestamps, maximum history window, and freshness threshold.
- Whether TrafficSignal rows map to Junctions or Devices; there is no current relationship.
- User/account lifecycle and role/permission policy; see ADR-002 and OSH-65/66.