# ARCH-001 — System overview

## Context

Several third-party consumer services worth automating have no public API, no
API keys and no OAuth. The only way to act on a user's account is to behave like
their browser: sign in with their own credentials and drive the same endpoints
the site's own single-page application calls. That capability is wanted from
more than one place — a Siri Shortcut, a shell script, and an AI agent all want
to fill the same grocery cart — and writing it once per caller is how the
implementation drifts.

This service exists so the capability is implemented once and exposed twice:
as a REST API for anything that speaks HTTP, and as a remote MCP server for
anything that speaks Model Context Protocol.

## Decision

A stateless .NET host with two transport surfaces over one shared service
layer, and a credential model in which the service itself holds nothing.

## Structure

### Four projects

The solution under `src/backend/` splits along a single axis: what is
provider-neutral, what is provider-specific, and what is transport.

| Project | Owns | Depends on |
| --- | --- | --- |
| `FrostAura.Integrations.Core` | The contracts every integration implements — `IIntegration`, `CredentialField`, `IntegrationCredentials`, the two credential exceptions — plus the provider-neutral shopping-list domain (`ShoppingListItem`, `Product`, `CartLineResult`, `CartBuildResult`, `ShoppingListParser`). | Nothing. |
| `FrostAura.Integrations.Woolworths` | The Woolworths integration: `WoolworthsIntegration` (the declaration), `WoolworthsClient` (an authenticated session), `WoolworthsSearchClient` (anonymous catalogue search), `WoolworthsCartService` (orchestration). | `Core`. |
| `FrostAura.Integrations.Api` | The host. Minimal-API endpoints under `/api`, MCP tools under `/mcp`, `CredentialAccessor`, and the exception-to-status-code mapping. | `Core`, `Woolworths`. |
| `FrostAura.Integrations.Tests` | xUnit. `Program` is declared `public partial` specifically so the host can be started in-memory. | All of the above. |

The split earns its keep in two places. First, `Core` depending on nothing is
what lets a second integration be added without touching Woolworths code —
the shopping-list domain and the parser are retailer-neutral, and only
`WoolworthsSearchClient` knows how a Woolworths product is found. Second,
keeping `Api` as the only project that knows about HTTP headers means the
integration layer takes an `IntegrationCredentials` and has no opinion about
where it came from, which is precisely why the same service methods can serve
both REST and MCP.

### Two surfaces, one service layer

`Program.cs` maps REST endpoints under `/api` and calls `app.MapMcp("/mcp")`.
`WoolworthsTools` — the `[McpServerToolType]` — is a set of thin wrappers over
the exact same `WoolworthsSearchClient` and `WoolworthsCartService` methods the
minimal-API endpoints call. Nothing of substance lives in either surface.

| Capability | REST | MCP tool | Credentials |
| --- | --- | --- | --- |
| Discovery | `GET /api/integrations` | — | none |
| Health | `GET /api/health` | — | none |
| Catalogue search | `GET /api/woolworths/search` | `woolworths_search_products` | none |
| Resolve a list, dry run | `POST /api/woolworths/shopping-list/preview` | `woolworths_preview_shopping_list` | none |
| Resolve a list into the cart | `POST /api/woolworths/shopping-list/cart` | `woolworths_add_shopping_list_to_cart` | required |
| Add one product | `POST /api/woolworths/cart/items` | `woolworths_add_product_to_cart` | required |

The two anonymous capabilities are deliberate and load-bearing. Search and
preview run entirely against the third-party search provider (see
[ARCH-002](ARCH-002-woolworths-integration.md)) and never sign in, so an AI
agent that holds no credentials at all can still resolve a shopping list and
show its matches. Requiring an account for those would remove the only safe
entry point the service has.

The MCP server is registered as **stateless** (`WithHttpTransport(o =>
o.Stateless = true)`). This is correct because every tool call carries its own
credentials and completes in one round trip; there is no server-to-client
sampling or elicitation that would need a session held open.

The one place the surfaces legitimately differ is error reporting. REST failures
go through the `UseExceptionHandler` block in `Program.cs`, which maps
`MissingCredentialsException` and `ProviderAuthenticationException` to 401,
cancellation to 504, `HttpRequestException` to 502, and everything else to 500 —
and only surfaces the message text for the two credential cases, because those
describe the caller's own request rather than provider internals. MCP failures
must be thrown as `McpException`: the MCP SDK replaces arbitrary exception text
with a generic message so servers cannot leak internals, which would otherwise
leave a client that simply has not been configured with the credential headers
with no idea what is wrong. `WoolworthsTools.RequireCredentials` exists solely
to perform that translation.

### The credential model

**The service stores no credentials.** There is no configuration file of
accounts, no database, no cache, no "remember me". Every request that acts on a
user's account carries that user's credentials in its own headers, and those
values live only for the duration of that request. The security property this
buys is worth stating plainly: compromising the host yields no standing access
to anybody's account.

The mechanism is `IIntegration`. An integration declares itself — key, display
name, description — and lists the `CredentialField`s it needs, each binding a
stable short key the integration reads (`"password"`) to the HTTP header a
caller sets (`X-Woolworths-Password`), with a description, a `Required` flag and
a `Secret` flag. `CredentialAccessor` reads the inbound request headers, collects
every declared field, and throws `MissingCredentialsException` naming the exact
missing headers if a required one is absent. That message is written for someone
wiring up an MCP client who cannot see the code.

Each integration declaring its own fields is the point. What Woolworths needs is
an account login, because Woolworths offers nothing else; the next integration
may need an API key, a bearer token, a store id, or all three. There is no
shared credential shape to force them into.

Discovery falls out of the same abstraction. `GET /api/integrations` enumerates
every `IIntegration` registered in DI and returns each one's credential fields,
headers and descriptions. Registration in `Program.cs` is what makes an
integration exist — there is no central list to keep in sync, and adding one
surfaces it to REST callers and MCP clients simultaneously. Note the two-line
registration pattern: the concrete type is registered as a singleton, then
registered *again* as `IIntegration` resolving to the same instance, so tools
can inject the concrete type while discovery still enumerates it.

### Session lifecycle

An authenticated Woolworths session is a `WoolworthsClient`, which owns its own
`CookieContainer` and is `IDisposable`. Sessions follow one shape and only one:

1. **Sign in.** `WoolworthsCartService` constructs a fresh `WoolworthsClient` for
   the operation and calls `SignInAsync` with the credentials resolved from this
   request's headers.
2. **Do the work.** All cart mutation for this one call.
3. **Sign out**, in a `finally`, so a run that fails part-way still ends its
   session.

**Sessions are never pooled, cached, or shared across requests.** This is not an
efficiency oversight. Authentication upstream is entirely cookie-based — no
bearer token, no `Authorization` header — so a pooled client is a container of
one specific person's cookies. Reusing it across callers would serve one user's
cart to another. The cost is a sign-in round trip per operation; that is the
correct price.

Sign-out is best-effort by design. `SignOutAsync` catches its own exceptions and
returns `false` rather than throwing, so a failed logout can never mask the
result of the work that preceded it. It also verifies rather than assumes: after
the POST it re-reads `/server/currentUser` and only considers the session ended
when `loggedInStatus` is `0`, logging a warning otherwise.

## Invariants

- **Both surfaces call the same service methods.** A behaviour added to one must
  not diverge from the other. If a tool starts doing its own orchestration, that
  logic belongs in the service layer instead.
- **No credential is ever stored, logged, cached or echoed back.** The password
  field is marked `Secret`; discovery returns the flag, never a value.
- **Anonymous endpoints stay anonymous.** Search and preview must never begin
  requiring an account.
- **Every authenticated operation signs out in a `finally`.**
- **The service must never be exposed over plain HTTP.** Credentials travel in
  headers, and TLS is the only thing protecting them.
- **No checkout.** This service fills a cart and stops. Placing a completion or
  payment step behind an automated call is out of scope deliberately, not
  incidentally.

## Alternatives considered

- **A single surface with an adapter.** Rejected: an MCP tool needs its
  descriptions written for a model rather than for a human reader, and the
  error-reporting contracts genuinely differ. Sharing the service layer captures
  all of the benefit; sharing the transport would have captured none of it.
- **Server-side stored accounts.** Rejected. It would remove per-request header
  plumbing from every caller, and in exchange make the host a credential store
  worth attacking. Adding it later requires an explicit recorded decision.
- **A pooled, kept-alive upstream session.** Rejected on correctness, per the
  cookie-container argument above.
- **EF Core and PostgreSQL, per the division default.** Rejected: the service is
  stateless by design and has nothing to persist. This departure is recorded as a
  decision in `MEMORY.md`, not normalised silently.

## Consequences

- Adding an integration is a matter of implementing `IIntegration`, writing its
  services, registering them, and adding a tool class. Discovery, credential
  resolution and error mapping come for free.
- Every authenticated operation pays a sign-in and a sign-out. Latency is the
  price of the isolation property.
- Callers carry the burden of holding credentials, which is where that burden
  belongs, but it does mean an MCP client must be configured with per-user
  headers rather than being genuinely multi-tenant.

## Fragility and unverified assumptions

The entire Woolworths integration drives undocumented, unversioned endpoints
captured from live browser traffic. Nothing about them is contractual and none
of it can be pinned to a version. [ARCH-002](ARCH-002-woolworths-integration.md)
covers the specific fragilities. The general posture that follows from it: this
service must fail loudly. A silent partial success — the natural failure mode of
these endpoints, which return HTTP 200 for almost everything — is worse than an
error, because the user believes they have a full cart.

The credential model itself is verified only to the extent that the Woolworths
integration exercises it. `CredentialField.Required = false` (optional fields)
and the `Secret` flag's effect on any future logging are declared but not yet
exercised by a real integration.

## Validation

- **Automated:** `FrostAura.Integrations.Tests` currently covers
  `ShoppingListParser` only — it is the one part of the pipeline testable without
  reaching Woolworths. `Program` is `public partial` so the host can be started
  in-memory, but the surface-level tests that would use it are not yet written.
- **Manual:** `make smoke` exercises the read-only endpoints of a running
  instance. Anything touching a cart requires a real account and a real
  observable cart.

## References

- [ARCH-002 — Woolworths integration](ARCH-002-woolworths-integration.md)
- [UC-001 — Cookidoo shopping list to cart](../use-cases/UC-001-cookidoo-shopping-list-to-cart.md)
- `src/backend/FrostAura.Integrations.Api/Program.cs`
- `src/backend/FrostAura.Integrations.Core/Credentials.cs`
- `src/backend/FrostAura.Integrations.Api/CredentialAccessor.cs`
