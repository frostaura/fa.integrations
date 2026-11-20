---
name: technologies-fa-integrations-decisions
description: "No DB (stateless, so no credential store exists to compromise); per-request header credentials; one cart item per request forced by upstream; Woolworths first behind an IIntegration seam"
type: decision
last_verified: 2026-08-21
---

# Decisions

## No database, no EF Core — 2026-08-21
The division default is .NET + EF Core + PostgreSQL. This service departs from
it deliberately: it is a stateless pass-through holding no user data, no
credentials and no cart state, because the cart lives on the provider's side
against the caller's own account.

**Why it matters beyond tidiness:** adding a database would create a credential
store that is currently, deliberately, absent. The absence *is* the security
property. Revisit only if a feature genuinely needs persistence — caching search
results would be the plausible first reason, and that wants a cache, not a
relational database.

## Credentials arrive per request as headers, never stored — 2026-08-21
Founder's call, made while the service was being designed. Each integration
declares its own credential fields via `IIntegration`; the caller sets them as
HTTP headers on every request; they live only for that request.

**Why:** makes the service multi-tenant with no user model at all, and means a
compromise of the host leaks no standing access to anyone's account.
**Cost, accepted:** every write operation pays a fresh sign-in — roughly one
extra round trip per call. Sessions are never pooled, so this cannot be
optimised away without giving up the isolation that makes it safe.

## One cart item per HTTP request — 2026-08-21
Forced by upstream behaviour, not style. Woolworths' `cartAddItems` accepts an
items array, but a batch containing a single unavailable product adds **nothing
at all** while returning HTTP 200 with a success-shaped body. Observed directly:
a 13-item batch with one out-of-stock product produced an empty cart and a
success response.

Costs one request per ingredient (~15 for a typical recipe). **Do not
"optimise" this back into a batch** — the failure mode is silent and total.

## Woolworths first, behind an integration seam — 2026-08-21
The `IIntegration` abstraction was introduced before a second integration
existed, on the founder's instruction that each integration should carry its own
auth scheme. This is not speculative generality: it is why the project is called
`integrations` rather than `woolworths`, and it means the discovery endpoint and
credential resolution need no central registry to keep in sync.

## Related
[Current state](state.md) · [Open questions](questions.md)
