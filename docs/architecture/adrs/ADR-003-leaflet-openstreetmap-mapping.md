# ADR-003: Leaflet and OpenStreetMap Mapping

- **Status:** Accepted direction: Leaflet + OpenStreetMap; coordinate model and production tile provider remain open
- **Date:** 2026-11-09
- **Issues:** [OSH-57](https://github.com/oshango/kintraffic/issues/57), [OSH-58](https://github.com/oshango/kintraffic/issues/58), [OSH-75](https://github.com/oshango/kintraffic/issues/83)

## Context

OSH-57 currently asks for an Azure Maps-versus-Leaflet comparison. The product direction in OSH-74 is Leaflet rendering with OpenStreetMap geographic data. The repository has no current map component or mapping package, and the API does not provide authoritative Junction coordinates or an operational definition of actively monitored status.

### Confirmed repository facts

- Angular 22 uses a feature-oriented standalone component structure. `package.json` has no Leaflet, Azure Maps, or chart dependency.
- The C# `Junction` model contains `Id`, `Name`, nullable free-form `Location`, and `CreatedAt`; it has no latitude/longitude or geometry property.
- `Device` has a required `JunctionId` relationship in EF Core and a free-form `Status` string. Status vocabulary and last-seen semantics are not defined.
- `Telemetry` has a required `DeviceId` relationship, a free-form `Metric`, numeric `Value`, and `RecordedAt`; it has no units or direct `JunctionId`.
- `RoadSegment.Geometry` is stored as a string (`nvarchar(max)` on SQL Server). There is no geometry-format, coordinate-reference-system, or coordinate-range validation.
- `TrafficSignal` is not linked to Junction or Device. Its relationship to monitored infrastructure remains unresolved.

## Decision

Use **Leaflet** as the frontend map-rendering library and **OpenStreetMap-derived geographic data** as the map-data ecosystem. Azure Maps is not a required runtime dependency. Keep the tile provider configurable so the production provider can be selected without rewriting the map component.

For new geographic data, recommend WGS 84 / EPSG:4326. Represent a Junction's point separately from its human-readable location description; represent road paths as validated GeoJSON using RFC 7946 coordinate order `[longitude, latitude]`. Validate latitude in `[-90, 90]` and longitude in `[-180, 180]`. These are recommended API/storage shapes, not current repository facts. Do not reinterpret or overwrite existing `Junction.Location` or `RoadSegment.Geometry` values until their contents and a migration/compatibility path are approved.

The product distinguishes:

1. **Geographic intersection:** a location present in the geographic base data; it is not evidence that KinTraffic manages infrastructure there.
2. **Registered Junction:** a persisted KinTraffic `Junction` record.
3. **Actively monitored Junction:** a registered Junction with an explicitly supported relationship to monitored Devices and an authoritative, sufficiently fresh status signal.

The current model/API can identify registered Junctions and their related Devices, but does not define the Device status vocabulary, telemetry freshness threshold, or a TrafficSignal-to-Junction relationship. Therefore no map/API implementation may infer “active monitoring” from a map intersection, a non-empty status string, or the mere presence of historical telemetry. These semantics require owner approval and explicit API data.

## Tile Provider Considerations

OpenStreetMap data and the OpenStreetMap Foundation's public standard tile servers are separate concerns. The public tile policy requires visible attribution, a valid web referrer, and cache compliance; prohibits bulk downloading and offline tile use; and provides best-effort service without an SLA. See the [OSMF Tile Usage Policy](https://operations.osmfoundation.org/policies/tiles/).

Do not assume `tile.openstreetmap.org` is suitable for production traffic. Select a commercial OSM-derived tile provider or a self-hosted service after estimating interactive usage, availability/SLA, cost, update responsibility, privacy, and operations. Keep tile URLs configurable, show provider attribution, and do not prefetch tiles beyond normal interactive use.

## Security and Performance Considerations

- Operational Device/Fault status may be sensitive; map data must be served under the approved API authentication/authorization policy from OSH-65/66 and must not expose internal data through tile URLs.
- Leaflet rendering should be lazy-loaded with the map route/feature and disposed cleanly. Avoid adding clustering or other plugins until realistic device volume demonstrates a need.
- Provide an accessible list/detail alternative, keyboard/touch operation, visible status text/icons, and explicit unknown/stale/offline states; color alone is insufficient.
- Do not claim real-time marker updates until OSH-73 defines event delivery, authorization, reconnection, and freshness.

## Alternatives Considered

- **Azure Maps as the required renderer/provider:** not selected because the approved product direction is Leaflet with OSM data, and no Azure mapping dependency is required for local development. A hosted commercial tile provider may still be selected independently.
- **Directly store coordinates in the existing `Location` string:** rejected because the field is free-form, nullable, and has no validation or coordinate-format contract.
- **Treat every mapped intersection as a monitored Junction:** rejected because geographic data does not prove a KinTraffic Junction or Device is registered there.
- **Use the public OSM tile service as the assumed production provider:** rejected because its policy is usage-limited and it has no SLA.

## Consequences and Trade-offs

- Leaflet works with the existing Angular architecture without requiring Azure and supports standard tile, marker, and GeoJSON layers.
- OSM attribution and tile-provider policy must remain visible and be reviewed for the chosen production service.
- Existing location/geometry strings are not sufficient for trustworthy coordinate-based map rendering; schema/API changes need an explicit migration plan.
- Map work can proceed after OSH-75 publishes approved geographic and monitoring fields; production provider selection may remain blocked on usage and availability requirements.

## Dependencies

- **OSH-57:** Implement the Leaflet map using the accepted OSM direction; document the production tile provider when approved.
- **OSH-58:** Keep the grid/table or list/detail view as an accessible equivalent and preserve identical operational fields.
- **OSH-75:** Publish Junction/Device/Telemetry contracts and explicit location/status semantics after decisions are approved.
- **OSH-73:** Define realtime updates before describing the map as live.
- **OSH-65/66:** Define authenticated access and backend-enforced permissions for operational status.

## OPEN — REQUIRES APPROVAL

- Authoritative source and backfill process for coordinates of existing Junctions.
- Whether Junction positions use a dedicated latitude/longitude value object or GeoJSON Point in the API/storage contract; preserve descriptive `Location` separately.
- GeoJSON validation and migration plan for existing `RoadSegment.Geometry` strings.
- Approved Device status vocabulary and the data required to call a Junction actively monitored.
- Telemetry freshness threshold, last-seen semantics, and units for each metric.
- Production tile provider, expected traffic, availability target, budget, update cadence, and operational owner.
- Whether and how legacy `TrafficSignal` records map to Junction/Device; no relationship is assumed here.

No frontend map or domain-model changes are implemented by this ADR.