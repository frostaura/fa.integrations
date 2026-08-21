# MEMORY — Integrations

- [Current state](memory/state.md) — Builds, runs and previews 15/15 locally; **not a git repository**, never containerised, never deployed, authenticated write path untested through the service
- [Decisions](memory/decisions.md) — No DB by design (no credential store exists to compromise); per-request header credentials; one cart item per request forced by upstream; Woolworths first behind an `IIntegration` seam
- [Gotchas](memory/gotchas.md) — `MSBuildEnableWorkloadResolver=false` is mandatory here; `dotnet new` exits 73 while succeeding; upstream returns HTTP 200 for every kind of failure
- [Open questions](memory/questions.md) — Does a headless-filled cart show in the Woolworths app; no rate limiting on a third-party key that is not ours; no agreement or notice channel with Woolworths
