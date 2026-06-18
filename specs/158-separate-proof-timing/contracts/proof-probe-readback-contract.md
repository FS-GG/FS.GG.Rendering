# Contract: Proof/Probe Readback

## Scope

This contract preserves screenshot/readback validation for correctness proof and explicit probe
runs while excluding that readback from accepted performance timing samples.

## Proof Path

Correctness proof may continue to use the existing live proof/readback commands and artifacts,
including Feature 155 and Feature 157 proof sets. Feature 158 readiness links those artifacts as
proof references but does not count their readback samples as timing samples.

When Feature 158 adds proof-specific routing, it uses:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-live-proof --feature 158 --out specs/158-separate-proof-timing/readiness/proof-probes
```

## Probe Mode

An explicit probe can be requested from the Feature 158 performance command:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 158 --probe-readback \
  --out specs/158-separate-proof-timing/readiness/timing
```

Probe mode may perform screenshot/readback validation and publish readback artifacts. Every sample
from probe mode must be classified as `probe-readback-included` and excluded from performance
acceptance with reason `probe-run-excluded`.

## Required Probe Fields

- Probe id.
- Command and options.
- Host profile.
- Scenario ids.
- Readback artifact paths.
- Probe sample ids.
- Measurement policy: `probe-readback-included`.
- Exclusion reason: `probe-run-excluded`.
- Diagnostics.

## Validation Rules

- Proof/probe readback evidence may support correctness or measurement diagnostics only.
- Proof/probe evidence must not be included in the accepted timing set.
- Failed proof readback records zero accepted proof artifacts and zero accepted performance
  artifacts.
- Cross-profile proof/probe artifacts may be linked as excluded evidence but cannot support
  same-profile acceptance.
- Unsupported-host proof/probe output is `environment-limited` and records zero accepted artifacts.

## Readiness Links

Readiness must link proof/probe artifacts from the timing summary and final validation summary so a
reviewer can confirm proof remains available without reconstructing timing inclusion from raw
files.
