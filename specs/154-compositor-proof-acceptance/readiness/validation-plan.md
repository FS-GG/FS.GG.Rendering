# Feature 154 Validation Plan

Status: `defined`

## Gates

- Real capable-host proof evidence is required before proof acceptance.
- Synthetic evidence is allowed only for rejection and environment-limited tests.
- Unsupported-host validation must record zero accepted partial-redraw artifacts.
- Same-profile parity must cover all ten required damage scenarios.
- Timing must declare threshold and noise policy before any performance benefit is accepted.
- Package, regression, MVU/effect-boundary, and per-story compile-registration tests must pass or record explicit limitations.

## Synthetic Disclosure

Synthetic Feature154 tests are named with `Synthetic` and use `// SYNTHETIC:` comments at each direct synthetic fixture use site.
