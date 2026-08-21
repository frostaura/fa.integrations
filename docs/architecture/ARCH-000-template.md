# ARCH-000 — Architecture Document Template

Copy this file to `ARCH-NNN-<slug>.md`, take the next free number, and replace
every section. Delete a section only when it genuinely does not apply, and say
why rather than leaving it empty.

## Context

What problem or constraint forced this piece of the system to exist, and what
was true before it did. Enough for a reader who has never opened the code.

## Decision

The shape actually chosen, stated in the present tense as a description of the
system rather than as a proposal.

## Structure

The components involved, what each owns, and how they call one another. Name
the real types, projects and endpoints so a reader can navigate from this
document straight into the source.

## Invariants

The properties that must hold. These are the rules a future change is most
likely to break by accident, so state each one as an imperative and say what
goes wrong when it is violated.

## Alternatives considered

- **Option:** why it was not taken.

## Consequences

- **Positive:** what this buys.
- **Negative:** what it costs, and what it makes harder later.

## Fragility and unverified assumptions

Anything that depends on undocumented third-party behaviour, anything observed
once and not re-tested, and anything believed but not proven. Be explicit about
which is which — a reader must be able to tell a verified fact from an
inference.

## Validation

- **Automated:** the tests that would fail if this decision were undone.
- **Manual:** the checks a human runs, and against what.

## References

- Related use cases:
- Related testing plans:
- Source files:
