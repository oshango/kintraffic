# ADR-004: First-Party JWT and Refresh Sessions

- **Status:** Proposed
- **Date:** 2026-11-14
- **Approver:** [name/title]
- **Approval date:** [date]
- **Related issue:** OSH-65
- **Related decision:** [ADR-002: KinTraffic-Managed Application Identity](ADR-002-kintraffic-managed-identity.md)

## Context

OSH-65 requires managed Identity accounts, browser refresh sessions, and API bearer authentication. ADR-002 recommends OAuth/OIDC with OpenIddict and prohibits issuing tokens directly from username/password. The owner-accepted deviation (approver and date to be recorded) for this implementation specifies first-party JWT issuance from Identity credentials. This is a material protocol deviation; ADR-002 retains its original decision, with a cross-reference to this proposal.

## Decision

Use ASP.NET Core Identity Core with a role-free Identity context. Keep credentials and Identity account state separate from `UserProfile`. Verify passwords with Identity, apply configured lockout-on-failure, and return uniform authentication failures. The disabled-account target below requires a separate helper; it is not implemented yet.

Issue first-party RS256 JWT access tokens with a `kid`, validated for issuer, audience, signature, pinned algorithm, and lifetime. The payload is limited to `sub`, `iss`, `aud`, `iat`, `nbf`, `exp`, and `jti`; do not include roles or profile data. Keep signing material external to committed files and support current and previous verification keys.

Issue opaque refresh tokens with at least 256 bits of randomness. Deliver them only in a Secure, HttpOnly cookie; persist SHA-256 token hashes, family expiry/revocation state, a hash of the Identity security stamp, and concurrency state. Rotate on each redemption using a transactional conditional update; a replay revokes the family. Password changes and logout revoke affected families. Refresh/logout require an exact allowed Origin, Fetch Metadata, and a custom request header.

Require explicit runtime configuration for token validation/lifetimes, cookie attributes, origins, signing keys, rate limits, trusted proxies, and Identity lockout and password settings. Refresh cookies assume a same-site UI/API topology. Refresh and logout require `Sec-Fetch-Site` to be present and equal to `same-origin` or `same-site`; missing, `cross-site`, and `none` values are rejected. Do not trust forwarded IP headers unless exact proxies are configured. Source-IP and normalized-account/IP throttles are in-memory and per application instance; account partitions are bounded. Do not log credentials, tokens, cookies, stamps, or throttle partition keys.

Do not protect existing operational controllers or define role policies here. Development account provisioning requires an explicit command and Development environment; it never runs during application startup.

## Decision Table

Delegated decisions record the intended policy, not completed deployment or implementation. Approver: [name/title]; decision date: [date]. This ADR remains Proposed until accepted.

| Item | Status | Decision or owner input needed |
| --- | --- | --- |
| 1. Topology | Needs owner input | Supply the owned registrable domain, DNS/certificate ownership, and choose same-site custom `app`/`api` subdomains or a same-origin API proxy. Both Azure resources support custom domains, but none are configured. Production Angular currently calls the `azurecontainerapps.io` API directly while Static Web Apps uses `azurestaticapps.net`; the checked-in Nginx proxy is local Docker only. |
| 2. Refresh cookie | Decided (owner-delegated) | Use `__Host-kt-refresh; Secure; HttpOnly; SameSite=Strict; Path=/` with no Domain. The API can express it and validates `/` for `__Host-` names; it does not force this name or SameSite value. Browser use remains blocked until item 1 resolves the current cross-site hosts. |
| 3. Allowed origins | Decided (owner-delegated) | Configure exactly one exact HTTPS UI origin per environment; the owner must supply each origin after item 1. Current API validation requires exact HTTPS values outside Development and CORS uses explicit values, but the options list permits multiple origins and startup does not enforce a count of one. Development may omit origins, disabling CORS. |
| 4. Access token | Decided (owner-delegated) | Configure a 10-minute lifetime and 60-second skew. These are configuration values, not code defaults. |
| 5. Refresh lifetime | Decided (owner-delegated) | Configure an 8-hour idle lifetime and a 24-hour absolute lifetime. Allow concurrent sessions with one family per login and no application-level cap. Assess unattended use before rollout. |
| 6. Signing keys | Needs owner input | Supply Key Vault resource/secret or key identifier, runtime identity access, secure key delivery approach, and ownership of 90-day rotation with at least 11 minutes overlap. RS256 3072-bit is supported; the current validator enforces a 2048-bit minimum, accepts PEM via configuration, and does not load Key Vault or manage rotation. |
| 7. Provisioning, recovery, MFA | Decided (owner-delegated) | Target Administrator-only provisioning with an explicitly guarded one-shot bootstrap using a deployment secret and refusing an existing Administrator; Administrator reset must force password change and revoke sessions. Require TOTP for Administrator and Supervisor before production. Roles, bootstrap, reset, forced-change and MFA are follow-up work, not implemented. |
| 8. Password policy | Decided (owner-delegated) | Adjust to 15–128 characters with no composition requirements while MFA is absent; configure IdentityV3 PBKDF2-HMAC-SHA512 at least 220,000 iterations. OWASP currently recommends 220,000 for this PRF and treats fewer than 15 characters as weak without MFA. Common-password blocking remains a follow-up. |
| 9. Lockout and throttling | Decided (owner-delegated) | Configure 10 consecutive failures and a 5-minute fixed lockout; 20 attempts per source IP per 5 minutes and 5 per normalized account/IP per 15 minutes. Configure partition cap and idle eviction explicitly; idle eviction must be at least 15 minutes so active windows cannot reset early. Current options validation does not enforce this relationship. Limits are per instance. |
| 10. Trusted proxies | Needs owner input | Supply verified immediate ingress peer IPs, actual forwarding topology, and network-isolation evidence. No Container Apps VNet/subnet CIDR is configured here; current code accepts exact proxy IPs, not CIDRs. Azure ingress appends `X-Forwarded-For`; only the rightmost address is provided by Container Apps, and other values require validation. Forward limit is 1. A subnet range is not currently supported by this configuration and must not be trusted without proving it contains only trusted proxies. |
| 11. Disabled accounts | Decided (owner-delegated) | Adjust to a future far-future Identity lockout with `LockoutEnabled=true` plus security-stamp update and session revocation. Refresh checks both lockout and stamp. This does not distinguish disabled from ordinary lockout and is not implemented; define helper, re-enable semantics and administrative controls before use. |
| 12. Federation / protocol | Decided (owner-delegated) | No Entra federation now; use the first-party JWT deviation under the documented controls. Record approver/date; ADR-002's OIDC direction is not silently superseded. |

## Still Open

- OSH-65 acceptance criterion 1 remains unmet until the provisioning, recovery and MFA decisions are formally recorded and their required go-live work is delivered.
- Record the owner-accepted first-party JWT deviation's approver and date; keep the ADR status Proposed until that record is complete.
- Resolve the production UI/API topology and configure the resulting exact allowed origin, cookie domain/SameSite settings, and ingress proxy peers.
- Define the signing-key secret delivery and rotation operation; the checked-in infrastructure does not configure Key Vault or API authentication secrets.
- Approve Administrator bootstrap, recovery, forced password reset, and MFA requirements before production account provisioning.
- Define disabled-account and re-enable semantics; no dedicated lifecycle helper is implemented.
- Confirm token lifetime, concurrent-session, revocation, and post-logout access-token expectations.
- Live access JWTs remain valid until expiry. Angular in-memory token handling belongs to OSH-67; endpoint authorization policy selection belongs to OSH-66/OSH-70.
- No Key Vault, API secret references, environment VNet/subnet, or per-environment auth settings are defined in the checked-in Terraform. Production cannot be assumed configured from this decision record.
- Identity password maximum length is enforced when setting/changing passwords; existing credentials are not truncated or rejected during verification. The login request's existing required-field validation rejects whitespace-only passwords even though the Identity policy does not. Benchmark the 220,000 iteration floor for operational capacity.
