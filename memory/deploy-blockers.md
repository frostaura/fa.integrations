---
name: fa-integrations-deploy-blockers
description: "Six Actions secrets unset so both image builds fail at Docker Hub login; no Portainer stack exists so PORTAINER_STACK_ID has no value yet; the built image predates the placeId fix"
type: watch
last_verified: 2026-08-21
---

# Deploy blockers

CI run [32464144857](https://github.com/frostaura/fa.integrations/actions/runs/32464144857) on the initial push:

| Job | Result |
| --- | --- |
| Backend (restore, build, test) | ✅ success |
| Security Scan | ✅ success |
| Build (amd64) / Build (arm64) | ❌ `Username and password required` |
| Create Multi-arch Manifest / Deploy Portainer | skipped |

**Cause: the repo has no secrets set.** `frostaura` is a user account, not an organisation, so there are no org-level secrets to inherit — every repo carries its own. Secrets are write-only and cannot be copied between repos. `fa.lifeos` has the full set and is the reference for where the values came from.

```
gh secret set DOCKERHUB_USERNAME    --repo frostaura/fa.integrations
gh secret set DOCKERHUB_TOKEN       --repo frostaura/fa.integrations
gh secret set PORTAINER_URL         --repo frostaura/fa.integrations
gh secret set PORTAINER_TOKEN       --repo frostaura/fa.integrations
gh secret set PORTAINER_ENDPOINT_ID --repo frostaura/fa.integrations
gh secret set PORTAINER_STACK_ID    --repo frostaura/fa.integrations
```

**Order matters.** `PORTAINER_STACK_ID` has no value yet — the deploy job assumes a Portainer stack already exists for this service and none does. Create the stack first, or the deploy job fails even once the other five are set. `crusaders.mainframe` is blocked on the identical six secrets and the identical missing stack, so both are one piece of infrastructure work, not two.

**The container image on disk predates the placeId fix.** Rebuild before any deploy — a deploy of the current image ships the silent-failure write path.

## Related
[Current state](state.md) · [Gotchas](gotchas.md)
