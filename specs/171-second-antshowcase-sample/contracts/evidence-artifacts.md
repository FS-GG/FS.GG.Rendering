# Contract: Evidence Artifacts

## Evidence Root

Feature readiness evidence is written under:

```text
specs/171-second-antshowcase-sample/readiness/
```

Ad hoc runs may write under:

```text
artifacts/second-ant-showcase/
```

Paths in committed readiness summaries should be project-relative unless an external process path is necessary for diagnostics.

## Required Readiness Files

```text
readiness/
|-- coverage.md
|-- interaction-review.md
|-- evidence-summary.md
|-- evidence-summary.json
|-- visual-review-summary.md
|-- visual-review-summary.json
|-- visual-findings.md
|-- limitations.md
|-- command-log.md
|-- preferred/
`-- minimum/
```

## Evidence Summary JSON

The aggregate JSON must include:

- `runId`
- `seed`
- `command`
- `startedAtUtc`
- `elapsedMs`
- `sampleVersion`
- `catalogCount`
- `pageCount`
- `coverageStatus`
- `interactionStatus`
- `templateStatus`
- `themeSwitchStatus`
- `visualReviewStatus`
- `unresolvedFindingCount`
- `environmentLimitations`
- `syntheticEvidence`
- `artifacts`
- `diagnostics`

Rules:

- Dynamic fields such as timestamps must be isolated.
- Synthetic or environment-limited evidence must be explicit.
- Overall accepted status requires clean coverage, passing interaction/template/theme checks, live complete visual targets, reviewer classifications, and zero unresolved findings.

## Coverage Evidence

`coverage.md` must include:

- exact command
- catalog count
- catalog page count
- template page count
- missing ids
- duplicated ids
- unknown ids
- final status

The coverage command must fail non-zero when status is not clean.

## Interaction Evidence

`interaction-review.md` must include:

- script seed
- pages exercised
- interaction contracts exercised
- before/after visible outcomes
- display-only demonstrations and reasons
- template interactions
- theme-switch preservation checks
- limitations

## Visual Evidence

Visual evidence must include:

- screenshot or degraded records for every required target
- contact sheets when screenshots exist
- reviewer classifications
- unresolved and closed findings
- environment limitation disclosure

The artifact model must distinguish these statuses:

- accepted
- blocked
- review-required
- environment-limited
- degraded
- failed

`environment-limited` and `degraded` cannot be summarized as accepted visual fidelity.

## Determinism Contract

Running the representative evidence path twice with the same seed must produce the same:

- page list
- interaction script steps
- interaction outcomes
- coverage result
- visual target ids
- limitation classification
- pass/fail summary, excluding explicitly dynamic fields

## Failure Diagnostics

Commands must name actionable causes for:

- missing .NET SDK
- missing or stale local package feed
- unknown page id
- unknown theme alias
- invalid size
- coverage drift
- interaction contract drift
- missing live visual environment
- screenshot capture failure
- unresolved visual findings
- malformed reviewer findings file
