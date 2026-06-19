# Contract: Guidance Rule Coverage

## Required Rules

| Rule id | Theme | Required references |
|---------|-------|---------------------|
| `package-pin-drift` | Package-consuming samples check current `FS.GG.UI.*` package pins and use local-feed proof. | `FS.GG.UI.*`, `scripts/refresh-local-feed-and-samples.fsx` or `package-feed`, stale package pins, local feed |
| `readiness-allowlisting` | Committed readiness evidence is ignored by default until allowlisted. | `specs/*/readiness/`, `.gitignore`, `git check-ignore` |
| `validation-output-isolation` | Same project/configuration validation is not parallelized unless output paths are isolated. | `dotnet test`, same project/configuration, isolated output or `BaseOutputPath` |
| `visual-readiness` | Real screenshots, degraded capture disclosure, reviewer classification, and summary caveat preservation are required. | screenshot, degraded, reviewer, accepted readiness, generated summary or managed section |
| `responsiveness-diagnostics` | Interactive readiness validates pointer and keyboard activation separately and separates routing from update/render/present latency. | pointer, keyboard, responsiveness, routing, render or present |
| `post-merge-package-bump` | Merge/post-merge work records package bump, local-feed pack, sample pin alignment, restore/validation, and readiness ledger updates. | package bump, local feed, sample package pins, restore or validation, readiness ledger |
| `evidence-honesty` | Canceled, timed-out, skipped, synthetic, substitute, degraded, pending-review, and environment-limited checks are visibly caveated. | canceled or timed out, synthetic or substitute, environment-limited or pending-review, caveat |

## Applicable Skills

The initial implementation must apply rules to at least these canonical skill
families:

- implementation and Spec Kit implementation guidance: all evidence-honesty,
  validation-output-isolation, readiness-allowlisting, and package-pin rules
- sample/generated product guidance: package-pin, local-feed proof,
  responsiveness, visual readiness, and evidence honesty
- testing and visual-readiness guidance: visual-readiness,
  responsiveness-diagnostics, validation-output-isolation, and evidence honesty
- merge/post-merge guidance: post-merge-package-bump, package-pin, local-feed
  proof, and evidence honesty
- package-owned skills that mention readiness or sample validation: relevant
  package-pin, readiness-allowlisting, validation-output-isolation, and evidence
  honesty caveats

## Coverage Status

| Status | Meaning |
|--------|---------|
| `covered` | Required references and guidance are present in a relevant canonical skill or inherited through a valid wrapper target. |
| `partial` | Some references are present, but an expected concrete command/path/status caveat is missing. |
| `missing` | A required rule is absent from a relevant skill. |
| `not-applicable` | The rule does not apply to the skill's domain. |
| `excepted` | A specific intentional exception explains why the rule does not apply or differs. |

## Passing Requirements

- Every required rule has at least one canonical source with `covered` status.
- Every relevant updated skill is `covered`, `not-applicable`, or `excepted` for
  each rule.
- Wrappers inherit coverage only from valid canonical targets.
- `partial` and `missing` statuses appear in the report with remediation hints.
- Exceptions do not hide broken target paths or stale wrapper metadata.

## Report Requirements

The report includes a coverage matrix:

```text
| Rule | Covered | Partial | Missing | Excepted | Not applicable |
```

Each missing or partial row links to the affected skill path and names the
expected reference or guidance phrase.
