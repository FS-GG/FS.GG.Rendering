# Contract: Layer Promotion Command

## Command

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-promotion --feature 159 \
  --out specs/159-layer-promotion-keys/readiness/promotion \
  --policy layer-promotion-v1 \
  --attempts 3
```

## Options

- `--feature 159`: selects the Feature 159 scenario set and evidence renderers.
- `--out <path>`: output directory for promotion attempts, reuse records, counters, parity, and
  unsupported-host reports.
- `--policy layer-promotion-v1`: required policy id for accepted evidence.
- `--attempts <n>`: number of fresh same-profile attempts to collect. Accepted readiness requires
  at least `3`.
- `--scenario <id>`: optional single scenario for focused debugging; cannot by itself satisfy
  final readiness.
- `--disable-promotion`: optional oracle/debug mode that forces lower safe tiers and records zero
  accepted promotion artifacts.

## Required Scenarios

- `promotion/static-retained`
- `promotion/placement-only-move`
- `promotion/scroll-shifted`
- `promotion/nested-retained`
- `promotion/content-change`
- `promotion/churn-demotion`
- `promotion/fallback-safe`

Additional scenarios may be published, but they cannot replace required scenarios.

## Outputs

The command writes:

```text
promotion/
|-- attempts/
|-- reuse/
|-- demotions/
|-- fallbacks/
|-- parity/
|-- unsupported/
`-- summary.md
```

Machine-readable `summary.json` may be added beside `summary.md` when the implementation chooses a
stable schema. If present, Markdown remains the reviewer entry point and JSON remains a derived
artifact.

## Acceptance Rules

- Accepted command output requires policy id `layer-promotion-v1`.
- Accepted attempts require host profile `probe-08a47c01` or a later accepted same-profile proof.
- At least three fresh same-profile attempts are required before Feature 159 can be `accepted`.
- Unsupported-host command output must complete with zero accepted reuse or promotion artifacts.
- Cross-profile, stale, missing-policy, missing-retained-content, parity-failing, resource-limited,
  or non-beneficial attempts must be rejected, demoted, bypassed, or fallback-only.
- The command never sets the shipped compositor performance claim to accepted by itself.

## Exit Behavior

- Successful evidence generation exits `0` for accepted, non-beneficial, fallback-only, rejected,
  or environment-limited status when artifacts are written correctly.
- CLI usage errors exit non-zero before writing accepted artifacts.
- Host or environment limitations produce an `environment-limited` artifact with zero accepted
  reuse or promotion artifacts.
