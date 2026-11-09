# ADR-002: KinTraffic-Managed Application Identity

- **Status:** Proposed; account lifecycle decisions require approval
- **Date:** 2026-11-09
- **Issues:** [OSH-65](https://github.com/oshango/kintraffic/issues/65), [OSH-66](https://github.com/oshango/kintraffic/issues/66), [OSH-67](https://github.com/oshango/kintraffic/issues/67)

## Context

KinTraffic needs authenticated operator access, but its current repository contains only a demo login response and a client-side placeholder guard. OSH-65 currently makes Microsoft Entra ID mandatory, while the product direction is for KinTraffic to own application accounts. The existing OSH-62 domain baseline separates business User/Role concepts from credentials and authorization policy.

### Confirmed repository facts

- `AuthController.Login` accepts no credentials and returns the literal `demo-token` and a hard-coded `admin` role.
- There is no JWT bearer registration, authentication middleware, `[Authorize]` use, refresh-token storage, or identity persistence model in the API.
- `UserProfile` is an ID, username, and string-array of roles; it is not included in `KinTrafficDbContext` and has no identity schema.
- Angular's `authGuard` reads token and roles from `localStorage`, but current application routes do not attach the guard. `LoginPageComponent` is presentation-only.
- `SecurityService` requests `/api/auth/current-user`, but the API does not expose that endpoint.
- The API already uses SQL Server and EF Core, so application account persistence can remain in the modular monolith and local development can use the existing local SQL workflow.

## Decision

**Recommended direction:** KinTraffic owns its application-user lifecycle. Microsoft Entra ID is not a mandatory runtime dependency; federation may be considered later as an explicit enterprise option.

Use the first-party ASP.NET Core Identity framework for credential verification, password hashing, account lockout, and persisted user/role records rather than implementing a bespoke password store. Keep the existing business `UserProfile` concept separate from credential records and define an explicit stable mapping between them. The exact entity mapping and role lifecycle require review before implementation.

The API will authenticate credentials, issue short-lived signed access tokens, and validate issuer, audience, signature, and lifetime. Angular will hold access tokens in memory, never in `localStorage` or `sessionStorage`. Refresh tokens will be high-entropy, stored server-side only as hashes, rotated on use, revocable, and checked for reuse. Refresh tokens will be delivered only in `Secure`, `HttpOnly` cookies. Cookie `SameSite`, CSRF-token handling, and accepted origins must be chosen against the actual deployment topology; credentialed CORS must use explicit origins and never `*`.

Authorization is enforced by ASP.NET Core policies, not by Angular route guards or client-supplied role fields. Protected API access is deny-by-default; only explicitly approved endpoints are anonymous. Angular route guards complement this policy, and the API remains authoritative for every operation.

## Security Considerations

- Avoid persistent browser storage for bearer credentials. In-memory access tokens reduce persistence but do not protect against active same-origin script execution; XSS prevention, dependency hygiene, CSP review, and careful rendering remain necessary.
- Cookies are sent automatically by browsers. Refresh/logout flows require CSRF protections appropriate to cross-site deployments, strict Origin validation, explicit credentialed CORS, and secure cookie attributes.
- Refresh-token hashes must not be logged. Rotation, revocation, replay detection, expiration, and key rotation require tests and audit events that exclude credentials.
- Login and refresh endpoints need rate limiting and safe, non-enumerating failures. Account recovery and enrollment must not leak whether an account exists.
- Never trust a client-provided `roles` array or user ID as proof of identity or authorization. Derive claims from the authenticated server-side identity and policy.
- Keep signing keys and other secrets in deployment configuration/secret stores. Local development must use local configuration and must not require Azure.
- Decide explicitly whether health/readiness endpoints are public; do not make operational data endpoints anonymous by default.

## Alternatives Considered

- **Microsoft Entra ID as the required identity provider:** rejected as the default because it conflicts with the stated KinTraffic-managed account direction and would make local/offline development depend on Azure identity configuration. It remains a possible federation option subject to a separate decision.
- **Custom password storage and authentication code:** rejected because password hashing, recovery, lockout, token replay, and account lifecycle are security-sensitive capabilities already handled by established ASP.NET Core Identity mechanisms.
- **Browser localStorage for access and refresh tokens:** rejected because persistent script-readable storage increases credential exposure after XSS.
- **Long-lived access tokens without refresh:** rejected because they make revocation and session termination ineffective for too long.

## Consequences and Trade-offs

- KinTraffic owns account provisioning, recovery, revocation, operational support, signing-key operations, and incident response.
- The API needs identity storage and token endpoints; Angular must coordinate refresh-cookie requests and in-memory access-token renewal.
- Secure cookies/CSRF and separate frontend/API deployment origins require explicit configuration and browser integration tests.
- Enterprise federation remains possible later, but must map external identities into the same approved application-user and authorization model.

## Implementation Dependencies

- **OSH-65:** Persist managed application identities; implement credential verification, access-token issuance/validation, refresh rotation/revocation, and account lifecycle after decisions below are approved.
- **OSH-66:** Define role/permission vocabulary and backend authorization policies; protect endpoints deny-by-default.
- **OSH-67:** Implement Angular login/session/refresh/sign-out behavior, in-memory access tokens, guarded routes, and an API-origin-restricted interceptor.
- **OSH-64:** Supply production SQL/network configuration and secret storage; local development remains Azure-independent.
- **OSH-68 / OSH-70 / OSH-72:** Align validation, ProblemDetails, rate-limited safe failures, API exposure, and security scanning.

## OPEN — REQUIRES APPROVAL

- Account provisioning: administrator-only invitation, operator-managed provisioning, or any self-registration.
- Account recovery and verification: approved email/SMS/provider, support process, and assurance level.
- Whether MFA is required for administrators, all users, or neither in the initial release.
- Approved role vocabulary, whether roles alone are sufficient or permissions are also required, and who may grant/revoke each role.
- Access-token and refresh-token lifetimes, rotation/reuse response, concurrent-session limits, and revocation expectations.
- Cookie domain, `SameSite` value, CSRF mechanism, and exact allowed origins for local, staging, and production deployments.
- Whether Entra federation is an accepted future integration and which external identity claims may map to KinTraffic users.

No authentication or authorization implementation is performed by this ADR.