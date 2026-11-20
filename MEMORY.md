# MEMORY — technologies.fa.integrations

- [Current state](memory/state.md) — Retired 2026-08-21, capability moved whole into technologies.fa.gaia; repo renamed and public, kept for provenance; write path was proven 9/9; never deployed
- [Decisions](memory/decisions.md) — No DB (stateless, so no credential store exists to compromise); per-request header credentials; one cart item per request forced by upstream; Woolworths first behind an IIntegration seam
- [Gotchas](memory/gotchas.md) — MSBuildEnableWorkloadResolver=false is mandatory in this checkout; dotnet new exits 73 while succeeding; upstream returns HTTP 200 for every kind of failure
- [Open questions](memory/questions.md) — Does a headless-filled cart show in the Woolworths app; no rate limiting on a third-party key that is not ours; no agreement or notice channel with Woolworths at all
- [Deploy blockers](memory/deploy-blockers.md) — Moot since the 2026-08-21 retirement: no Actions secrets are set, so the image builds still fail at Docker Hub login and nothing publishes or redeploys on push
