# Contract: Verdict Record

**Feature**: `176-test-antshowcase-controls`

Defines the per-control verdict record and the completeness rule: exactly one classified record per
cataloged control, none left unexercised or unclassified.

## Record schema

See the **Control Verdict Record** entity in [data-model.md](../data-model.md) for the full field
list. The binding shape:

```text
ControlVerdictRecord =
  { ControlId; Family; PageContext
    Classification: Interactive | DisplayOnly
    ClassificationReason            // required for DisplayOnly
    BehaviorsExercised: BehaviorOutcome list
    InteractionStates: InteractionStateOutcome list
    VisualEvidence: VisualEvidenceItem list
    DamageEvidence: DamageOutcome list
    FunctionalVerdict: Pass | Fail | NeedsReview | EnvironmentLimited | NotApplicable
    VisualVerdict:     Approved | NeedsReview | Blocked | EnvironmentLimited
    Findings: string list
    Diagnostics: string list }
```

## Completeness rule (the core of US1)

- **C-1**: `{ record.ControlId } == CoverageMap.catalogIds ()` — set equality, no missing, no
  duplicate (FR-006, SC-001).
- **C-2**: every record is terminal: `Classified` or `EnvironmentLimited`. No record may be emitted
  as `Unexercised`/`Unclassified` (data-model VR-2).
- **C-3**: `DisplayOnly` ⇒ non-empty `ClassificationReason`; such a record is **never** reported as
  "failed functionality" for lacking a state change (Edge Cases — display-only) — its
  `FunctionalVerdict = NotApplicable`.
- **C-4**: `Interactive` ⇒ `FunctionalVerdict ∈ {Pass; Fail; NeedsReview; EnvironmentLimited}` and
  `BehaviorsExercised` covers every documented behavior (FR-002, SC-002).

## Verdict semantics

| Verdict | Meaning |
|---------|---------|
| `Pass` | All driven behaviors produced the expected state change; visual states differ from rest. |
| `Fail` | A documented behavior produced no/incorrect state change, or a required state didn't differ. |
| `NeedsReview` | Functionally responded but a fidelity/aesthetic question needs human sign-off. |
| `EnvironmentLimited` | A live-only check couldn't run for lack of a window (FR-008). |
| `NotApplicable` | Functional dimension of a display-only control. |

## Determinism

Records are byte-stable across same-seed/same-build runs; wall-clock fields are confined to a
non-asserted metadata block (research §D6). Behavior ordering follows the catalog order, not
discovery order.

## Test obligations

- The record-per-control set equality (C-1) and terminal-classification (C-2) checks are the primary
  `ControlPassCoverageTests` assertions.
- `DisplayOnly` reason presence (C-3) and `Interactive` behavior coverage (C-4) are asserted per
  control against the interaction contract.
