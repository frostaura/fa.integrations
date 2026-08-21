---
name: fa-integrations-state
description: "Scaffolded 2026-08-21, builds and runs locally, 15/15 preview verified; not a git repo, never containerised, never deployed, authenticated write path untested through the service"
type: state
last_verified: 2026-08-21
---

# Current state

_Last verified: 2026-08-21 by direct inspection — build run, tests run, service
started locally and every endpoint below called against the live Woolworths
catalogue._

Created 2026-08-21 in the same session that reverse-engineered the Woolworths
endpoints. It exists, it builds, and it works locally. Nothing beyond that.

**Not a git repository.** No `git init`, no remote, no commits. This is the
single most important fact about the project right now: every artifact in it
exists only on this machine, inside an iCloud-synced directory. There is no
`integrations.frostaura.net` DNS record or Cloudflared route either — the
hostnames in `CLAUDE.md` are the intent, not a live service.

## Verified by inspection

| Check | Result |
| --- | --- |
| `make build` | succeeds (.NET 10, `.slnx` solution) |
| `make test` | 12 parser tests, all pass |
| `GET /api/health`, `GET /api/integrations` | correct |
| `GET /api/woolworths/search` | live results with ZAR prices |
| `POST /api/woolworths/shopping-list/preview` | **15/15** ingredients resolved from the sample Cookidoo share |
| MCP `initialize` / `tools/list` / `tools/call` | all work over streamable HTTP at `/mcp`; 4 tools exposed |
| Missing credential headers | HTTP 401 on REST; a message naming the exact headers on MCP |
| `docker build` | succeeds from `src/backend/Dockerfile` with the repo root as context |
| Container run | serves health, search and MCP on `:8080`, as non-root `app` (uid 1654) |

## Not yet verified — do not claim these

- **The authenticated write path has never run through this service.**
  `cartAddItems` was proven by hand against the live site during
  reverse-engineering, and the C# mirrors that exactly, but no shopping list
  has been added to a real cart *via this API*. First thing to test.
- The GitHub Actions workflow has never executed. `ubuntu-24.04-arm` runner
  availability and the Docker Hub / Portainer secrets are all unconfirmed. The
  `deploy-portainer` job additionally assumes a Portainer stack already exists
  for this service; none does yet.
- Only `linux/arm64` has been built and run, natively on Apple silicon. The
  amd64 leg — which is what actually runs in production — is unproven.

## Related
[[fa-integrations-decisions]] · [[fa-integrations-gotchas]] · [[fa-integrations-questions]]
