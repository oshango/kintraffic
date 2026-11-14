# ADR-004: First-Party JWT and Refresh Sessions

- **Status:** Proposed
- **Date:** 2026-11-14
- **Approver:** [name/title]
- **Approval date:** [date]
- **Related issue:** OSH-65
- **Related decision:** [ADR-002: KinTraffic-Managed Application Identity](ADR-002-kintraffic-managed-identity.md)

## Context

OSH-65 requires managed Identity accounts, browser refresh sessions, and API bearer authentication. ADR-002 recommends OAuth/OIDC with OpenIddict and prohibits issuing tokens directly from username/password. The direction authorized for this implementation instead specifies first-party JWT issuance from Identity credentials. This is a material protocol deviation; ADR-002 remains unchanged and the approving authority/date must be recorded before this proposal is accepted.

## Decision

Use ASP.NET Core Identity Core with a role-free Identity context. Keep credentials and Identity account state separate from `UserProfile`. Verify passwords with Identity, apply configured lockout-on-failure, and return uniform authentication failures. No disabled-account rule is defined; disabled-account enforcement remains open until an account lifecycle decision exists.

Issue first-party RS256 JWT access tokens with a `kid`, validated for issuer, audience, signature, pinned algorithm, and lifetime. The payload is limited to `sub`, `iss`, `aud`, `iat`, `nbf`, `exp`, and `jti`; do not include roles or profile data. Keep signing material external to committed files and support current and previous verification keys.

Issue opaque refresh tokens with at least 256 bits of randomness. Deliver them only in a Secure, HttpOnly cookie; persist SHA-256 token hashes, family expiry/revocation state, a hash of the Identity security stamp, and concurrency state. Rotate on each redemption using a transactional conditional update; a replay revokes the family. Password changes and logout revoke affected families. Refresh/logout require an exact allowed Origin, Fetch Metadata, and a custom request header.

Require explicit runtime configuration for token validation/lifetimes, cookie attributes, origins, signing keys, rate limits, trusted proxies, and Identity lockout settings. Do not trust forwarded IP headers unless exact proxies are configured. Source-IP and normalized-account/IP throttles are bounded in-memory partitions; limits are per application instance. Do not log credentials, tokens, cookies, stamps, or throttle partition keys.

Do not protect existing operational controllers or define role policies here. Development account provisioning requires an explicit command and Development environment; it never runs during application startup.

## Open Decisions and Limitations

- Record the approver and date for the first-party JWT deviation from ADR-002; decide whether OIDC federation is a future requirement.
- Approve account provisioning ownership, recovery/verification, and MFA. OSH-65 acceptance criterion 1 remains unmet until provisioning, recovery, and MFA decisions are approved.
- Define any disabled-account lifecycle and enforcement rule; none is implemented today.
- Approve deployment-specific origins, cookie SameSite/domain choices, token lifetimes, signing-key rotation operations, and concurrent-session/revocation expectations.
- Live access JWTs remain valid until expiry. Built-in throttles are per instance. Angular in-memory token handling belongs to OSH-67; endpoint authorization policy selection belongs to OSH-66/OSH-70.
