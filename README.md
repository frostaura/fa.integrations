> **Retired 2026-08-21.** This project is no longer maintained or deployed. Its capability — the Woolworths integration, the REST endpoints and the MCP tools — lives on in [`frostaura/technologies.fa.gaia`](https://github.com/frostaura/technologies.fa.gaia). This repository is kept public for provenance only.

# FrostAura Integrations — `technologies.fa.integrations`

A stateless gateway that exposes third-party consumer services as **both** a
REST API and a **remote MCP server**, so the same capability is reachable by a
Siri Shortcut, a script, or an AI agent without being written twice.

- REST — `https://integrations.frostaura.net/api`
- MCP — `https://integrations.frostaura.net/mcp`

> Operating context for agents is in [`CLAUDE.md`](CLAUDE.md); current state,
> decisions and known traps are in [`MEMORY.md`](MEMORY.md). Neither hostname
> above was ever made live, and since the retirement neither ever will be.

## The idea

The service stores **no credentials**. Every request carries the identity it
should act as, in headers, and each integration declares which fields it needs.
A compromise of the host therefore leaks no standing access to anyone's account,
and the service is multi-tenant without having a user model at all.

Ask it what it can do:

```bash
curl -s https://integrations.frostaura.net/api/integrations
```

## Integrations

### Woolworths South Africa

Grocery search and cart filling, built for the flow *share a shopping list from
the Cookidoo (Thermomix) app → get a filled Woolworths cart*.

| Header | Value |
| --- | --- |
| `X-Woolworths-Username` | account email |
| `X-Woolworths-Password` | account password (plaintext over TLS; base64-encoded onward, never stored) |

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/woolworths/search?q=&limit=` | none | Search the catalogue |
| `POST /api/woolworths/shopping-list/preview` | none | Resolve a list to products, change nothing |
| `POST /api/woolworths/shopping-list/cart` | headers | Resolve a list and fill the cart |
| `POST /api/woolworths/cart/items` | headers | Add one product by id |

MCP tools: `woolworths_search_products`, `woolworths_preview_shopping_list`,
`woolworths_add_shopping_list_to_cart`, `woolworths_add_product_to_cart`.

Try it without an account:

```bash
curl -sX POST http://localhost:5199/api/woolworths/shopping-list/preview \
  -H 'content-type: application/json' \
  --data '{"text":"150 g dried fusilli pasta\n0.5 red capsicum"}'
```

**This service fills carts. It never checks out** — nothing in it can place an
order or move money.

## Running it

```bash
gh repo clone frostaura/technologies.fa.integrations
cd technologies.fa.integrations
make build   # restore + build
make test    # xUnit suite
make run     # local API on :5199
make smoke   # hit the read-only endpoints of a running instance
make up      # container stack
```

Use the `Makefile` rather than raw `dotnet` — this checkout needs
`MSBuildEnableWorkloadResolver=false` and the Makefile exports it. See
[`memory/gotchas.md`](memory/gotchas.md).

## Connecting an MCP client

Point the client at `https://integrations.frostaura.net/mcp` (streamable HTTP)
and set the credential headers on the connection. Tools that need no credentials
— search and preview — work without them, so an agent holding no secrets can
still be useful.

## Documentation

- [`docs/architecture/ARCH-001-system-overview.md`](docs/architecture/ARCH-001-system-overview.md) — system shape
- [`docs/architecture/ARCH-002-woolworths-integration.md`](docs/architecture/ARCH-002-woolworths-integration.md) — the reverse-engineered endpoint contract
- [`docs/use-cases/UC-001-cookidoo-shopping-list-to-cart.md`](docs/use-cases/UC-001-cookidoo-shopping-list-to-cart.md) — the end-to-end flow
- [`docs/use-cases/UC-002-siri-shortcut-setup.md`](docs/use-cases/UC-002-siri-shortcut-setup.md) — building the Shortcut

## A caveat worth reading

Woolworths publishes no API. Every endpoint here was established by observing
the live site, and the integration can break without notice. The architecture
docs record exactly what was observed and where the fragility is concentrated —
read `ARCH-002` before changing anything in the Woolworths client.
