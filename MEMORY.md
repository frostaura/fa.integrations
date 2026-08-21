# MEMORY — Integrations

- [Current state](memory/state.md) — Private repo pushed, ahead-only by context commits, one uncommitted CLAUDE.md edit; **write path proven end-to-end via a real MCP client (9/9)** after QA caught and fixed the missing-placeId bug; nothing deployed
- [Deploy blockers](memory/deploy-blockers.md) — Six Actions secrets unset so both image builds fail at Docker Hub login; no Portainer stack exists yet; the built image predates the placeId fix
- [Decisions](memory/decisions.md) — No DB by design (no credential store exists to compromise); per-request header credentials; one cart item per request forced by upstream; Woolworths first behind an `IIntegration` seam
- [Gotchas](memory/gotchas.md) — `MSBuildEnableWorkloadResolver=false` is mandatory here; `dotnet new` exits 73 while succeeding; upstream returns HTTP 200 for every kind of failure
- [Open questions](memory/questions.md) — Does a headless-filled cart show in the Woolworths app; no rate limiting on a third-party key that is not ours; no agreement or notice channel with Woolworths
