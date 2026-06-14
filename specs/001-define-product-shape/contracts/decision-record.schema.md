# Contract: Decision Record (`docs/product/decisions/NNNN-*.md`)

Lightweight ADR-style record for a product-shape decision. Two are produced in this feature.

## Required structure

```markdown
# NNNN. <Title>

**Status**: accepted | deferred
**Date**: YYYY-MM-DD

## Decision
<The definite choice. No "to be decided later".>

## Rationale
<Why this choice; reference constitution/plan where relevant.>

## Revisit trigger
<The condition or migration stage that reopens this decision.>

## Options considered
<Only required when Status is `deferred`: the options on the table.>
```

## Field rules

- **Status**: `accepted` for a settled choice; `deferred` for an explicit deferral (still a
  definite decision *to defer*, with options listed).
- **Decision / Rationale / Revisit trigger**: all required, all non-empty.

## Instances in this feature

| ID | Title | Expected decision |
|---|---|---|
| 0001 | Package identity | Keep `FS.Skia.UI.*`; rebrand deferred to Stage R8 (per constitution). Status `deferred`. |
| 0002 | Template ownership | Rendering repo owns templates for now. Status `accepted`. |

## Acceptance (maps to spec)

- [ ] Both records exist with a definite Decision (no unspecified gaps). *(FR-006, FR-007, SC-004)*
- [ ] Each has Rationale and a Revisit trigger. *(SC-004)*
- [ ] Package-identity record agrees with constitution (`FS.Skia.UI.*` initially). *(FR-006)*
