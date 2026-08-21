---
name: fa-integrations-state
description: "Created 2026-08-21; private repo pushed and fully synced (0/0); CI green on backend+security but image builds fail on missing Docker Hub secrets; write path still untested through the service"
type: state
last_verified: 2026-08-21
---

# Current state

_Last verified: 2026-08-21 by direct inspection — build run, tests run, service
started locally and in a container, every endpoint below called against the live
Woolworths catalogue, and git state re-checked against the remote._

Created 2026-08-21 in the same session that reverse-engineered the Woolworths
endpoints.

**Repository: `frostaura/fa.integrations`, private, pushed.** Branch `main`
tracks `origin/main` at **0 ahead / 0 behind**, working tree clean, no lock
debris, reflog shows a single initial commit with no resets. As of this pass it
is the only Technologies repo that is fully synced with its remote — everything
here is durable, not sitting on one Mac.

There is no `integrations.frostaura.net` DNS record or Cloudflared route yet —
the hostnames in `CLAUDE.md` are the intent, not a live service.

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

## CI — ran once, partially green

Run [32464144857](https://github.com/frostaura/fa.integrations/actions/runs/32464144857)
on the initial push:

| Job | Result |
| --- | --- |
| Backend (restore, build, test) | ✅ success |
| Security Scan | ✅ success |
| Build (amd64) / Build (arm64) | ❌ `Username and password required` |
| Create Multi-arch Manifest / Deploy Portainer | skipped |

**Cause: the repo has no secrets set.** `frostaura` is a user account, not an
organisation, so there are no org-level secrets to inherit — every repo carries
its own. `fa.lifeos` has the full set and is the reference.

**Fix — six secrets, values from the same place `fa.lifeos` got them:**

```
gh secret set DOCKERHUB_USERNAME   --repo frostaura/fa.integrations
gh secret set DOCKERHUB_TOKEN      --repo frostaura/fa.integrations
gh secret set PORTAINER_URL        --repo frostaura/fa.integrations
gh secret set PORTAINER_TOKEN      --repo frostaura/fa.integrations
gh secret set PORTAINER_ENDPOINT_ID --repo frostaura/fa.integrations
gh secret set PORTAINER_STACK_ID   --repo frostaura/fa.integrations
```

`PORTAINER_STACK_ID` has no value yet — the deploy job assumes a Portainer stack
already exists for this service and none does. Create the stack first, or the
deploy job will fail even once the other five are set.

## Not yet verified — do not claim these

- **The authenticated write path has never run through this service.**
  `cartAddItems` was proven by hand against the live site during
  reverse-engineering, and the C# mirrors that exactly, but no shopping list
  has been added to a real cart *via this API*. First thing to test.
- Only `linux/arm64` has been built and run, natively on Apple silicon. The
  amd64 leg — which is what actually runs in production — has never been built,
  because CI failed before reaching it.
- The `ubuntu-24.04-arm` runner has never actually executed a build step here
  (it failed at the Docker Hub login, which precedes the build).

## Related
[[fa-integrations-decisions]] · [[fa-integrations-gotchas]] · [[fa-integrations-questions]]
