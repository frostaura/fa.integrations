---
name: technologies-fa-integrations-deploy-blockers
description: "Moot since the 2026-08-21 retirement: no Actions secrets are set, so the image builds still fail at Docker Hub login and nothing publishes or redeploys on push"
type: watch
last_verified: 2026-11-20
---

# Deploy blockers

**Moot since the retirement — never clear them.** The project will not be deployed. As of 2026-11-20 the repo still has no Actions secrets (`gh secret list` returns none), so a push runs the backend and security jobs and then fails at the Docker Hub login: nothing is published and nothing redeploys. That failure is now the safety property; setting the secrets below would start publishing and redeploying a retired service. Kept as the record of why it never shipped.

CI run [32464144857](https://github.com/frostaura/technologies.fa.integrations/actions/runs/32464144857) on the initial push:

| Job | Result |
| --- | --- |
| Backend (restore, build, test) | ✅ success |
| Security Scan | ✅ success |
| Build (amd64) / Build (arm64) | ❌ `Username and password required` |
| Create Multi-arch Manifest / Deploy Portainer | skipped |

**Cause: the repo has no secrets set.** `frostaura` is a user account, not an organisation, so there are no org-level secrets to inherit — every repo carries its own. Secrets are write-only and cannot be copied between repos.

```
gh secret set DOCKERHUB_USERNAME    --repo frostaura/technologies.fa.integrations
gh secret set DOCKERHUB_TOKEN       --repo frostaura/technologies.fa.integrations
gh secret set PORTAINER_URL         --repo frostaura/technologies.fa.integrations
gh secret set PORTAINER_TOKEN       --repo frostaura/technologies.fa.integrations
gh secret set PORTAINER_ENDPOINT_ID --repo frostaura/technologies.fa.integrations
gh secret set PORTAINER_STACK_ID    --repo frostaura/technologies.fa.integrations
```

**Order matters.** `PORTAINER_STACK_ID` has no value yet — the deploy job assumes a Portainer stack already exists for this service and none does. Create the stack first, or the deploy job fails even once the other five are set.

**The container image on disk predates the placeId fix.** Rebuild before any deploy — a deploy of the current image ships the silent-failure write path.

## Related
[Current state](state.md) · [Gotchas](gotchas.md)
