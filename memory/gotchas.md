---
name: fa-integrations-gotchas
description: "MSBuildEnableWorkloadResolver=false is mandatory in this checkout; dotnet new exits 73 while succeeding; upstream returns HTTP 200 for every kind of failure"
type: gotcha
last_verified: 2026-08-21
---

# Gotchas

## The MSBuild workload resolver crashes in this checkout
Every `dotnet` invocation fails with an `MSB4242` stack trace out of
`WorkloadResolver.GetInstalledWorkloadPacksOfKind` until
`MSBuildEnableWorkloadResolver=false` is set. Almost certainly because the tree
sits under an iCloud-synced path.

The `Makefile` exports it for every target, so `make build` / `make test` work.
**A bare `dotnet build` from the shell will still fail** — that is the trap.
Nothing here uses workloads, so disabling the resolver costs nothing.

## `dotnet new` exits 73 while working correctly
Project templates in this tree return exit code 73 and print a templating
error link, having created every file correctly. Check the filesystem before
concluding the command failed and re-running it.

## The solution is `.slnx`
The newer XML solution format, same as `fa.guild`. Tooling expecting `.sln`
will not find it.

## Upstream returns HTTP 200 for failures
This is the single most dangerous property of the Woolworths integration, and
it applies to bad credentials, unavailable products, and a logout that did not
happen. Status codes are not a signal here; the body is. Full detail in
`docs/architecture/ARCH-002-woolworths-integration.md`.

The corollary for anyone writing a new integration: verify against the *system*,
not the status code. A green call proved the request was accepted, not that it
did anything.

## Related
[Current state](state.md) · [Decisions](decisions.md)
