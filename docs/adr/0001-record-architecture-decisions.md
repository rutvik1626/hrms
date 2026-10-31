# 0001 — Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-10-31

## Context
The HRMS is built incrementally, largely by Claude Code following `docs/prompts/`. Decisions made during the build, especially ones that change or refine `docs/HRMS-Requirements.md`, need to be written down where both people and Claude will find them later.

## Decision
We keep Architecture Decision Records in `docs/adr/`, one file per decision, numbered `NNNN-short-title.md`, using this template:

```markdown
# NNNN — Title
- **Status:** Proposed | Accepted | Superseded by NNNN
- **Date:** YYYY-MM-DD

## Context
What forces are at play; what problem we are solving.

## Decision
What we decided.

## Consequences
What becomes easier or harder; follow-up work.
```

When an ADR changes something stated in `docs/HRMS-Requirements.md`, the requirements doc is updated in the same change and links to the ADR.

## Consequences
- Every non-obvious decision has a short written rationale.
- Claude Code sessions can read past decisions instead of re-deciding them.
