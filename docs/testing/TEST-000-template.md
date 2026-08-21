# TEST-000 — Testing Plan Template

Copy this file to `TEST-NNN-<slug>.md`, take the next free number, and replace
every section. A testing plan says what is actually verified and — just as
importantly — what is not.

## Scope

The behaviour and surfaces this plan covers, and the use cases or architecture
documents it validates.

## Required gates

List only the gates that genuinely apply, and say what each runs. Delete the
rest rather than leaving them as aspirations.

- `build`
- `unit`
- `integration`
- `manual`

## Automated coverage

- **Unit:** which types, and which behavioural contract each test file carries.
- **Integration:** what is stood up, and how — note where the host is started
  in-memory versus where a real dependency is involved.

State the command that runs each. Prefer the `Makefile` target.

## Manual verification

Steps a human runs, in order, with the exact command or request and the expected
observable result. Say where the check is performed — a terminal, a browser, a
third-party account.

## What is not covered, and why

The gaps, named. Third-party behaviour with no sandbox, flows that need live
credentials, endpoints observed once and never re-tested. A plan that omits this
section is claiming coverage it does not have.

## Exit criteria

- The listed gates pass.
- The manual steps were run, not assumed.
- Documented behaviour and observed behaviour agree; drift is fixed in the same
  change, not deferred.

## References

- Related use cases:
- Related architecture:
- Fixtures:
