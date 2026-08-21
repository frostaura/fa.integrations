# UC-000 — Use Case Template

Copy this file to `UC-NNN-<slug>.md`, take the next free number, and replace
every section. A use case describes observable behaviour, not implementation —
link to the relevant `ARCH-NNN` for the mechanism.

## Goal

The outcome a user gets, in one or two sentences, from their point of view.

## Actors

- **Primary:** who initiates it, and through which surface (REST, MCP tool,
  shortcut, agent).
- **Secondary:** the third-party services involved and what each provides.

## Preconditions

What must already be true. Include credentials the caller must hold and any
one-time account setup that this flow does not perform itself.

## Main flow

Numbered steps, each stating what happens and what the caller sees. Name the
endpoints or tools invoked so the flow can be traced into the code.

## Alternate and failure flows

Each distinct way the flow can deviate, what the user experiences, and how the
result reports it. Cover the silent failures explicitly — a step that fails
while reporting success is the one worth writing down.

## What this deliberately does not do

Boundaries that look like omissions and are not. State the reason for each.

## Acceptance criteria

- [ ] Observable, checkable statements. Anything that can only be confirmed
      against a live third-party account should say so.

## Verified behaviour

What has actually been run end to end, against what input, with what result.
Separate this from what is expected but untested; do not blur the two.

## References

- Related architecture:
- Related testing plans:
- Fixtures:
