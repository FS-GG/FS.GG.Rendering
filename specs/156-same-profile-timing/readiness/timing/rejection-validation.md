# Feature 156 Rejection Validation

## Fail-Closed Coverage

- Noisy same-profile live measurements are retained but do not accept the shipped performance claim.
- Incomplete and missing sample paths map to `incomplete`.
- Non-beneficial damage-scoped samples map to `non-beneficial`.
- Unsupported-host execution maps to `environment-limited`.
- Proof-readback or validation-readback overhead maps to `limited`.
- Cross-profile or overclaiming package summaries are rejected by `CompositorTimingAssertions`.

## Synthetic Fixture Disclosure

- `tests/Testing.Tests/Feature156TimingHelperTests.fs` contains `// SYNTHETIC:` rejection-only verdict fixtures.
- Synthetic fixtures are used only for policy rejection coverage and do not stand in for live timing evidence.

## Result

- No noisy, incomplete, cross-profile, environment-limited, limited, or non-beneficial result is counted as positive timing evidence.
