# Comprehensive code and architecture review

- Repository: `FS-GG/FS.GG.Rendering`
- Reviewed revision: `1f0278415ed60ffb7b9ebe77ba82258329969570`
- Review completed: 2026-07-25 19:46:15 UTC (21:46:15 CEST)
- Scope: all source and test projects, public contracts, viewer/adaptor boundaries, current GitHub checks, and Release test behavior

## Executive assessment

Rendering has a credible layered architecture: scene and control models are separated from retained rendering, viewer runtime, platform adaptors, and optional integrations. The current default-branch checks are green. A full Release run exposed one reproducible suite-level race around the process-global shaping provider; the same test passes alone, identifying isolation rather than deterministic shaping behavior as the defect.

Overall risk: **medium**. The codebase is well defended by tests, but global mutable provider state, several very large modules, and explicitly pending suites make some changes harder to validate than the green CI surface suggests.

## Architecture

The repository separates portable contracts and scene/control state from concrete backends such as Skia, OpenGL, terminal, and viewer hosts. That dependency direction supports headless testing and multiple presentation environments. The Viewer and Controls/Elmish composition layers are the main complexity centers, while Audio remains an integration dependency rather than a rendering-core concern.

## Evidence

| Check | Result |
|---|---|
| Current-revision GitHub checks | 17 succeeded, 5 intentionally skipped, 0 failed |
| Full solution, Release | All suites passed except one `SkiaViewer.Tests` test |
| Failed test in isolation, Release | Passed |
| Failure | `Feature142 fallback diagnostics.clearing the provider is explicit` expected fallback but observed shaping |

The suite also reports deliberate pending/skipped coverage in documentation fences, Elmish performance/timing baselines, a typed-control contract, and environment-dependent viewer/smoke paths. This review was not a GPU compatibility matrix or performance benchmark.

## Findings

### 1. High — shaping-provider tests race through process-global mutable state

`tests/SkiaViewer.Tests/Feature142FallbackDiagnosticsTests.fs` clears the shaping provider and immediately expects fallback from `Text.shapeText`. During the full Release suite another test can install a provider between those operations. `Fonts.fs` locks individual install/clear operations, but the test's clear-then-shape sequence is not atomic.

The failed test passes when filtered and run alone, which makes the isolation defect reproducible at suite scope. It can hide real fallback regressions and creates configuration/machine-dependent CI behavior.

Recommendation: make provider selection an injected/scoped dependency for shaping operations. As an interim measure, put all tests that mutate the global provider in one non-parallel collection and restore prior state in a fixture.

### 2. Medium — complexity is concentrated in a small set of oversized modules

Examples include `ControlsElmish` (~2,873 lines), `ViewerRuntime` (~2,351), `OpenGl` (~2,078), and `RetainedRender` (~1,641). These modules span state transition, resource lifecycle, event routing, and backend concerns.

Recommendation: extract lifecycle/state machines behind narrow internal interfaces, prioritizing ViewerRuntime and ControlsElmish. Use existing tests as characterization coverage and avoid changing public scene contracts during the split.

### 3. Medium — several important suites are explicitly pending

The repository contains whole pending lists for documentation-fence drive tests and Feature 109 performance/timing baselines, plus smaller pending or capability-skipped tests. These are clearly labelled, which is good, but permanent skips can become invisible.

Recommendation: assign each non-environmental skip an issue and expiry/owner. Run performance and timing suites in a scheduled, controlled job rather than every PR.

### 4. Low — prior review debt remains visible at the public boundary

The preceding report identified a stale `global.json` claim and the apparently unused public `DiagnosticReadinessImpact` type. Neither is a current correctness failure, but both weaken contract clarity.

Recommendation: correct the documentation claim and either demonstrate/cover the public type's supported use or remove it in the next compatible breaking window.

## Strengths

- Portable scene/control contracts are cleanly separated from concrete rendering hosts.
- Headless and adapter-focused tests provide broad behavior coverage.
- Fallback diagnostics are modeled explicitly rather than inferred from missing output.
- Current GitHub checks are green across the multi-project matrix.
- Pending coverage is labelled rather than silently omitted.

## Recommended order

1. Remove or serialize global shaping-provider mutation and add a repeat/stress test.
2. Turn non-environmental skips into tracked scheduled work.
3. Split ViewerRuntime and ControlsElmish along lifecycle boundaries.
4. Close the remaining public-contract/documentation debt from the prior review.
