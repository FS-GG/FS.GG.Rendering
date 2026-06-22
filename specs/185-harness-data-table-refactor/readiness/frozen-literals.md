# Frozen Literals — Harness Data-Table Refactor (185)

These literals are CI-grepped / test-asserted and MUST stay **byte-identical** through every story
(FR-008, edge "Directory-path drift"). The semantic-diff harness enforces content/path identity
modulo timestamps; this list is the explicit human-readable inventory.

## 1. Directory / path byte-strings (behind the 110 `*ReadinessDirectory` constants)

All derive from `Path.Combine("specs", <slug>, "readiness", …)`. The descriptor helpers in
`FeatureCatalog.FeatureDescriptor` reproduce these **exactly** (verified: `readinessDirectory` =
`Path.Combine("specs", d.Slug, "readiness")`, identical to the old `feature###ReadinessDirectory`
since `feature###Id` = slug):

| Helper | Byte-string (relative) |
|---|---|
| `readinessDirectory d` | `specs/<slug>/readiness` |
| `variantDirectory LiveProof d` | `specs/<slug>/readiness/live-proof` |
| `variantDirectory Parity d` | `specs/<slug>/readiness/parity` |
| `variantDirectory Reuse d` | `specs/<slug>/readiness/reuse` |
| `variantDirectory Snapshot d` | `specs/<slug>/readiness/snapshots` |
| `variantDirectory Timing d` | `specs/<slug>/readiness/timing` |
| `compatibilityLedgerPath d` | `specs/<slug>/readiness/compatibility-ledger.md` |
| `validationSummaryPath d` | `specs/<slug>/readiness/validation-summary.md` |
| `packageValidationPath d` | `specs/<slug>/readiness/package-validation.md` |
| `regressionValidationPath d` | `specs/<slug>/readiness/regression-validation.md` |

The 12 slugs: `148-compositor-live-integration`, `149-complete-compositor-p7`,
`152-compositor-live-proof`, `153-compositor-proof-interpreter`, `154-compositor-proof-acceptance`,
`155-native-proof-capture`, `156-same-profile-timing`, `157-no-clear-damage-scissor`,
`158-separate-proof-timing`, `159-layer-promotion-keys`, `160-performance-validation-throughput`,
`161-host-performance-lane-ledger`.

## 2. Required report-header titles (test-asserted, byte-identical)

Static `# Feature <N> …` report titles emitted by the renderers and asserted by
`tests/Rendering.Harness.Tests/*`. Populated into `descriptor.RequiredHeaders` (US1 T010); the test
suites are retargeted to assert against that field (FR-010). Dynamic titles bearing F# interpolation
(`# Feature 156 Scenario: {…}`, `# Feature 158 Excluded Samples: {…}`) are NOT frozen literals — only
their stable prefix is, and they stay inline.

The static title set per feature is enumerated in `rehoming-map.md` §RequiredHeaders.

## 3. Shared scalar literals (must not drift)

- `sharedAcceptedProfileId = "probe-08a47c01"` (156–161 accepted-profile).
- Policy ids: `same-profile-live-threshold-v2` (156), `readback-free-timing-v1` (158),
  `layer-promotion-v1` (159), `focused-throughput-v1` (160), `host-lane-ledger-v1` (161).
