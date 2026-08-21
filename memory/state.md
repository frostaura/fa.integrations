---
name: fa-integrations-state
description: "Private repo pushed, 1 context commit ahead; write path proven via a real MCP client 9/9 after the placeId bug was caught and fixed; nothing deployed — see the deploy blockers"
type: state
last_verified: 2026-08-21
---

# Current state

_Last verified: 2026-08-21 by direct inspection — build run, tests run, service started locally and in a container, every endpoint below called against the live Woolworths catalogue, and git state re-checked against the remote (re-confirmed by the division audit later the same day)._

Created 2026-08-21 in the same session that reverse-engineered the Woolworths endpoints. It is the only project in the division built end to end inside a single session, and the only one whose first commit and last commit are the same day.

**Repository: `frostaura/fa.integrations`, private, pushed.** Branch `main` tracks `origin/main`, working tree clean, no lock debris, reflog shows no resets. The three product commits (scaffold → verified-state record → the placeId fix) are pushed and 0 behind; the 2026-08-21 division audit added a fourth, context-only commit, so the branch reads **1 ahead**. All the code is off-machine — this is one of only two Technologies repos where that is true.

**Nothing is deployed.** There is no `integrations.frostaura.net` DNS record and no Cloudflared route; the hostnames in `CLAUDE.md` are intent, not a live service. The deploy blockers are their own topic.

## Verified by inspection

| Check | Result |
| --- | --- |
| `make build` | succeeds (.NET 10, `.slnx` solution) |
| `make test` | 12 parser test cases (9 `[Fact]` + a 3-case `[Theory]`), all pass |
| `GET /api/health`, `GET /api/integrations` | correct |
| `GET /api/woolworths/search` | live results with ZAR prices |
| `POST /api/woolworths/shopping-list/preview` | **15/15** ingredients resolved from the sample Cookidoo share |
| MCP `initialize` / `tools/list` / `tools/call` | all work over streamable HTTP at `/mcp`; **4 tools** exposed (`search_products`, `preview_shopping_list`, `add_shopping_list_to_cart`, `add_product_to_cart`) |
| Missing credential headers | HTTP 401 on REST; a message naming the exact headers on MCP |
| `docker build` | succeeds from `src/backend/Dockerfile` with the repo root as context |
| Container run | serves health, search and MCP on `:8080`, as non-root `app` (uid 1654) |

## The write path is proven, and cost one real bug

Full local QA per `docs/testing/TEST-001-e2e-rest-and-mcp.md`: a 17-case curl matrix over REST (400s on malformed input, 401s naming the exact headers on all three partial-credential variants, clamps working), then a real `@modelcontextprotocol/sdk` client over streamable HTTP — **9/9 checks pass, including the authenticated write**. A shopping-list line was added to the real cart through the service via headers on the MCP connection: 14 → 15 items, R795.45, ~4s including sign-in and verified sign-out.

The first write attempt failed with `formexceptions: "Place Id is missing."` — the C# client had dropped `address.placeId`, invisible during reverse-engineering because the browser session had already been through the address modal. Fixed by resolving the account's default saved address after sign-in and sending it on every add; an account with no saved address now fails actionably instead of silently. Contract recorded in `ARCH-002`. **This is the canonical example of the repo's own rule: a green upstream response proves the request was accepted, not that it did anything.**

## Not yet verified — do not claim these

- Only `linux/arm64` has been built and run, natively on Apple silicon. The **amd64 leg — which is what actually runs in production — has never been built**, because CI failed before reaching it.
- The `ubuntu-24.04-arm` runner has never executed a build step here (it failed at Docker Hub login, which precedes the build).
- Whether a headless-filled cart appears in the Woolworths mobile app — see the open questions.

## Related
[Deploy blockers](deploy-blockers.md) · [Decisions](decisions.md) · [Gotchas](gotchas.md) · [Open questions](questions.md)
