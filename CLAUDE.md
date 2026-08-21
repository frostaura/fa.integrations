# Integrations — `fa.integrations`

## What this project is
A stateless integration gateway that exposes third-party consumer services as
both a REST API and a remote MCP server, so the same capability is reachable by
a Siri Shortcut, a script, or an AI agent without writing it twice. The first
integration drives Woolworths South Africa: search the grocery catalogue and
fill the customer's cart from a shared shopping list. The intended public
surface is `integrations.frostaura.net/api` and `integrations.frostaura.net/mcp`
— whether that hostname resolves yet is a question for `MEMORY.md`, not this
file.

## Architecture & key concepts
Four projects under `src/backend/`:

| Project | Role |
| --- | --- |
| `FrostAura.Integrations.Core` | Provider-neutral contracts: `IIntegration`, `CredentialField`, `IntegrationCredentials`, the shopping-list domain types, and `ShoppingListParser`. Depends on nothing. |
| `FrostAura.Integrations.Woolworths` | The Woolworths integration — session client, search client, cart orchestration. |
| `FrostAura.Integrations.Api` | The host. Maps REST under `/api`, MCP under `/mcp`, and resolves credentials from headers. |
| `FrostAura.Integrations.Tests` | xUnit. |

**Both surfaces call the same services.** MCP tools in `WoolworthsTools` are thin
wrappers over the identical service methods the minimal-API endpoints use. A
behaviour added to one must not diverge from the other.

**Every integration declares its own auth scheme.** An integration implements
`IIntegration` and lists the `CredentialField`s it needs, each bound to a
request header. `CredentialAccessor` resolves them per request. Adding an
integration therefore surfaces it in `GET /api/integrations` automatically —
there is no central registry to update.

**The service stores no credentials, ever.** Callers supply their own on every
request and they live only for that request's duration. This is the core
security property: a compromise of the host leaks no standing access to
anybody's account. Do not add credential persistence, a "remember me" mode, or
server-side default accounts without an explicit decision recorded in
`MEMORY.md`.

**Sessions are per-operation.** Sign in, do the work, sign out — in a `finally`.
Sessions are never pooled or cached across requests, so one caller's cookies
can never serve another's call.

## Stack & repo
.NET 10 minimal API, `ModelContextProtocol.AspNetCore` for the MCP surface.
No database and no EF Core: the service is stateless by design and has nothing
to persist. That is a deliberate departure from the division default, recorded
as a decision in `MEMORY.md`. No frontend — the division's `src/frontend/`
convention does not apply to a headless service. Repo:
`https://github.com/frostaura/fa.integrations` (`main`), private.

## Conventions & non-obvious rules
- **Add cart items one request at a time.** Woolworths' `cartAddItems` accepts an
  array, but a batch containing a single unavailable product adds *nothing*
  while still returning HTTP 200 with a success-shaped body. Batching here is a
  correctness hazard, not an optimisation.
- **Never trust an upstream HTTP status.** Woolworths returns 200 for bad
  credentials, for unavailable products, and for a logout that did not happen.
  Always inspect the body — `errorMessage`, `formexceptions`, `loggedInStatus`.
- **The search `filters[visibility]` parameter must be sent twice** (`all` and
  `web and app`). Sending only `web and app` returns zero *food* results while
  still returning non-food, which reads convincingly as a location-gated
  catalogue and is not.
- **`data.id` from search is the `catalogRefId` for add-to-cart.** That
  equivalence is what removes a lookup step; do not "fix" it by introducing one.
- **Anonymous endpoints must stay anonymous.** Search and preview need no
  account and must never start requiring one — they are what makes the service
  safe to expose to an AI agent that holds no credentials.
- **MCP errors need `McpException`.** The SDK replaces arbitrary exception text
  with a generic message, so a caller that simply has not configured its headers
  would otherwise be told nothing useful.
- **Parser localisation is load-bearing.** Cookidoo emits Australian/European
  product vocabulary. Without the synonym map, "capsicum" and "flat-leaf
  parsley" return nothing against the South African catalogue.

## Build, test, run
Use the `Makefile`; it exports the environment this checkout needs.

```
make build     # restore + build
make test      # xUnit suite
make run       # local API on :5199
make smoke     # hit the read-only endpoints of a running instance
make up/down   # container stack
```

## Docs
`docs/architecture/` (system shape and the Woolworths endpoint contract),
`docs/use-cases/`, `docs/testing/`, each with a `-000-template.md`.
`docs/fixtures/cookidoo-share-sample.txt` is a verbatim Cookidoo share used by
the tests and by manual smoke runs. The architecture docs are authoritative on
upstream behaviour — they encode traffic captured from the live site, which no
vendor documentation exists for.

## Never do here
- Never store, log, cache or echo back a caller's provider password.
- Never expose this service over plain HTTP; credentials travel in headers and
  TLS is the only thing protecting them.
- Never place a completion, payment, or checkout step behind an automated call —
  this service fills a cart and stops there, deliberately.
- Never batch cart additions (see above).
- Never let a failed sign-out mask the result of the work that preceded it.

## Dependencies on other FrostAura projects
None.

## Skills

From the installed Gaia plugins this headless service draws the mandated-stack
baseline (e.g. `fa-engineering-default-tech-stack`) for the .NET shape and for
the recorded no-database departure from it, the formal-testing capability for the
test layers (e.g. `fa-engineering-testing`), the implementation capability for
delivery (e.g. `fa-engineering-implementation`), and the **deploy-chain
activation** capability (e.g. `fa-engineering-deploy-chain`) for the Docker Hub
login this repo is stalled at — read it alongside the deploy-blockers topic in
`MEMORY.md`. The UI-engineering capability does not apply; there is no frontend.

**Reference a capability by what it does, and name the current skill as an example
only — never as the identifier, never by path.** Plugin skills are not files in this
repo, so nothing here links to them and no checker sees a rename; a bare name goes
dead silently. Whether a named skill is installed today is a `MEMORY.md` question.

This repo has **no local `.claude/skills/`**, has never had one, and must not acquire
one. It is the only Technologies repo that was never vendored, and also the only one
that showed no stack drift — that correlation is the argument, recorded at division
level. A procedure that would be equally correct in an unrelated repository belongs
upstream in a plugin.

## Upkeep
Three artifacts are kept current **as changes land**, not in a later pass: this file,
[`MEMORY.md`](MEMORY.md) with its `memory/` topic store, and the skills layer above
(empty by decision). `MEMORY.md` is required here exactly as `CLAUDE.md` is, and
cascades downward identically; a `CLAUDE.md` with no `MEMORY.md` beside it is a defect.

Update the memory store at the end of any session that shipped, decided,
discovered or abandoned something — and its "not yet verified" section is the
first thing to correct once a claim there becomes true. Restamp `last_verified:`
only for what you actually inspected. This file changes only when a rule
changes; a status belongs in `MEMORY.md`. Upstream behaviour that turns out to
differ from what `docs/architecture/` records is a blocking defect: fix the doc
in the same change as the code. The standard is
[`/docs/operating/context-cascade.md`](../../../docs/operating/context-cascade.md).
