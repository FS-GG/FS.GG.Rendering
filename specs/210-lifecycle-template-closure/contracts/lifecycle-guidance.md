# Contract: Consumer Lifecycle Guidance

Guidance added to `.template.package/README.md` (the published consumer guide). Converts the shipped-but-
undiscoverable `--lifecycle` parameter into an adoptable feature (FR-007..009, SC-004).

## Required content

### 1. Decision tree (FR-007)

Maps the three consumer scenarios to values — a reader picks correctly from this alone:

```text
What do you want to scaffold?
├─ A governed product with the Spec Kit lifecycle (specify/plan/tasks, constitution, agent context)
│     → --lifecycle spec-kit   (default; omit the flag for the same result)
├─ An app-only product to be composed by the SDD scaffold (external orchestrator supplies governance)
│     → --lifecycle sdd
└─ A bare standalone product with nothing attached
      → --lifecycle none
```

### 2. Per-value include / exclude (FR-007)

| value | includes | excludes |
|---|---|---|
| `spec-kit` (default) | the full Spec Kit lifecycle surface: `.specify/`, generated constitution, `.agents/`/`.claude/` skills + context, `AGENTS.md`/`CLAUDE.md` | — (full surface) |
| `sdd` | the generated product (app-only) | the entire gated lifecycle surface — an **external orchestrator** supplies lifecycle/governance |
| `none` | the generated product only | the gated lifecycle surface **and** any orchestrator expectation |

### 3. Standalone `none` statement (FR-008)

MUST state plainly: with `--lifecycle none`, **no governance and no orchestrator are attached, and none is
expected** — nothing will be added later. Prevents the predictable "I picked none and was surprised nothing
was attached" failure (spec edge case).

### 4. Migration note (FR-009)

For consumers on the pre-lifecycle template:
- the lifecycle-aware version is a drop-in upgrade;
- **select the default (`spec-kit`, or omit `--lifecycle`) to reproduce prior output byte-for-byte**;
- choose `sdd`/`none` only to opt out of the emitted lifecycle surface.

## Validation rules

- All three values appear in both the decision tree and the include/exclude table.
- The standalone-`none` statement uses unambiguous "none is attached AND none is expected" wording.
- The migration note explicitly names the default-reproduces-prior-output guarantee.
- SC-004: a first-time reader maps all three scenarios (governed / SDD-composed / standalone) to the correct
  value with no maintainer consultation.
