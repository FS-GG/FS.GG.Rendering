# Feature 159 Readiness Summary

Status: `accepted`
Policy id: `layer-promotion-v1`
Accepted host profile: `probe-08a47c01`
Measured host profile: `probe-08a47c01`
Accepted attempt count: `5`
Counter net saved work: `48`
Performance claim: `performance-not-accepted`

## Evidence Links

- Promotion summary: `promotion/summary.md`
- Attempts: `promotion/attempts/`
- Reuse: `promotion/reuse/README.md`
- Demotions: `promotion/demotions/`
- Fallbacks: `promotion/fallbacks/`
- Parity: `promotion/parity/`
- Unsupported host: `promotion/unsupported/validation.md`
- Counters: `counters/promotion.md`
- Compatibility ledger: `compatibility-ledger.md`
- Package validation: `package-validation.md`
- Regression validation: `regression-validation.md`
- FSI identity authoring: `fsi/content-placement-identity-authoring.fsx`
- FSI promotion authoring: `fsi/compositor-promotion-authoring.fsx`
- FSI readiness authoring: `fsi/compositor-readiness-authoring.fsx`

## Reviewer Checklist

- Required scenarios are listed in `promotion/summary.md`.
- Promotion decisions use stable tokens: `promoted`, `observing`, `kept`, `demoted`, `rejected`, `bypassed`, `non-beneficial`, `fallback-only`, `environment-limited`.
- Reuse decisions use stable tokens: `content-reused-placement-updated`, `content-recorded`, `content-re-recorded`, `fallback-full-redraw`, `reuse-rejected`, `environment-limited`.
- Unsupported-host evidence records accepted reuse artifacts `0` and accepted promotion artifacts `0`.
- Synthetic fixtures are limited to pure identity, promotion-policy, counter, and replay tests; audited test names include `Synthetic` and each fixture block carries a `// SYNTHETIC:` rationale.
- `performance-not-accepted` remains the shipped compositor performance claim.

## Validation Runs

- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-promotion --feature 159 --out specs/159-layer-promotion-keys/readiness/promotion --policy layer-promotion-v1 --attempts 3`: passed.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 159 --out specs/159-layer-promotion-keys/readiness`: passed.
- `env -u DISPLAY -u WAYLAND_DISPLAY dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-promotion --feature 159 --out specs/159-layer-promotion-keys/readiness/promotion/unsupported --policy layer-promotion-v1 --attempts 1`: passed, `environment-limited`.
- Focused Feature 159 tests: Controls passed 6, SkiaViewer passed 2, Rendering.Harness passed 6, Testing passed 3, Package passed 4.
- Feature 155/157/158/159 preservation filter: passed 27.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed.
- `git diff --check`: passed.

## Decision

- Feature 159 accepts only same-profile, parity-passing, net-positive promotion/reuse counters.
- Missing, stale, ambiguous, cross-profile, resource-limited, unsupported, or parity-failing evidence fails closed.
