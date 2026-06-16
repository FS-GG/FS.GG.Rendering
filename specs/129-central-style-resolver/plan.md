# Implementation Plan: Central Visual-State Style Resolver (`theme → kind → intent → states → style`) — Workstream F, Phase F4

**Branch**: `129-central-style-resolver` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/129-central-style-resolver/spec.md`

## Summary

F4 introduces the **front half** of style resolution — a single, total, deterministic path
`theme + control-kind + semantic-intent + visual-state(s) → ResolvedStyle` — and migrates the
Button to consume it, replacing the hardcoded `primary: bool` baseStyle dispatch in
`buttonGeom` (`src/Controls/Control.fs:816`). The intent, today lowered to a `style` attribute
string that **the renderer never reads** (so a `Danger` button renders byte-identically to
`Primary`), becomes a **consumed input** that reaches resolution.

The new logic composes on — and reuses verbatim — the shipped 093 back-half resolver
`Style.resolve theme baseStyle classes state` (`src/DesignSystem/Style.fsi:30`): the front half
supplies `baseStyle` from `(kind, intent)` under a **policy seam**, then hands off to
`Style.resolve` for the class+state overlay, preserving the 093/096 precedence unchanged.

Scope is **strictly behaviour-neutral under the default theme** (decided 2026-06-16): wiring the
resolver leaves today's rendered output **byte-identical** across every intent and visual state.
The default policy is intent-agnostic (ignores intent → reproduces today). Intent *divergence*
(e.g. a real `Danger` red) is proven reachable through a **non-default policy supplied directly
to the resolver in a test** (User Story 3), with zero edits to any control's render code — but it
is **not** switched on under the default theme. Like F1 (126) and F2 (127), the new code lands in
an **internal module reached by tests via `InternalsVisibleTo`**, with **zero public-surface
delta**; public promotion is deferred to F5.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, single solution `FS.GG.Rendering.slnx`,
`TreatWarningsAsErrors=true` (code must be warning-clean).

**Primary Dependencies**: None added (FR-009). Reuses existing in-repo assemblies only —
`FS.GG.UI.DesignSystem` (`Theme`, `ResolvedStyle`, `Style.resolve`, `StyleClass`, `StyleVariant`,
`VisualState`) and `FS.GG.UI.Controls` (`ButtonIntent`, `buttonGeom`, render dispatch). No JSON
parser, no web/React/DOM/icon-font dependency enters any product/test assembly.

**Storage**: N/A (pure in-memory style assembly; no persistence).

**Testing**: xUnit-style headless deterministic tier (no GL context required for parity/totality).
New tests land in `tests/Controls.Tests/Feature129CentralStyleResolverTests.fs`, reaching the
internal resolver via an `InternalsVisibleTo` grant. Full suite run via `FS.GG.Rendering.slnx`.

**Target Platform**: Linux (dev/CI) headless for the F4 gates; the resolver is platform-neutral.

**Project Type**: F# UI rendering framework (multi-project single solution). No new project.

**Performance Goals**: Neutral — the resolver touches *style assembly only*. At-rest, animation,
layout, identity, memoization, virtualization, picture/text caches, and fingerprint/replay
behaviour stay byte-identical (FR-011, SC-008). No new allocation on the settled path beyond the
two `ResolvedStyle` records `buttonGeom` already builds.

**Constraints**: Zero per-package public-surface-baseline delta and zero design-token-drift
delta (FR-007, SC-004). Unchanged test pass/skip counts (FR-010, SC-005). Total + deterministic
resolution over the full `{kind} × {intent} × {state}` cross-product (FR-004, SC-003). No `Theme`
record shape change (FR-012). No control forked per intent/theme (FR-008, SC-006).

**Scale/Scope**: One new internal module (~1 file, no `.fsi`), one IVT grant addition, a small
edit to `buttonGeom` + its dispatch + intent extraction in `faithfulContent`, and one new test
file. Button-first migration only; other controls already calling `Style.resolve` are untouched.

## Constitution Check

*GATE: evaluated before Phase 0 and re-checked after Phase 1 design. Constitution v1.0.0.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass | Spec done. **No `.fsi` change** (internal-additive, FR-007); the "FSI surface" sketch is the internal module signature exercised by `InternalsVisibleTo` tests — the same FSI-first discipline 126/127 used for deferred-promotion work. Parity/totality tests precede the `buttonGeom` migration. |
| II. Visibility lives in `.fsi`, not `.fs` | ✅ Pass | New code is `module internal StyleResolver` with **no `.fsi`**; visibility governed by the project file + IVT, no access modifiers on `.fs` top-level bindings. No public module gains/loses members. |
| III. Idiomatic simplicity | ✅ Pass | Plain total `match` over `kind`/policy record; no SRTP, reflection, custom operators, type providers, or non-trivial CEs. The policy seam is a one-field record holding a function — the minimal representation of "overridable mapping". |
| IV. Elmish/MVU boundary | ✅ N/A | No new stateful/I-O workflow. Resolution is a pure function; it rides existing control attributes through the already-Elmish render path. No `Model`/`Msg`/`Cmd` introduced. |
| V. Test evidence is mandatory | ✅ Pass | Parity test (byte-identity vs. pre-migration oracle), totality test (exhaustive cross-product, no exception), and divergence test (non-default policy) all fail-before/pass-after. Oracle is the pre-migration literal baseStyles — **real**, not synthetic; no `Synthetic` token needed. |
| VI. Observability & safe failure | ✅ Pass | Resolution is total — unknown kind/intent falls back to a defined style, never throws, never yields an empty/transparent style. No silent swallow. |

**Change classification**: **Tier 2 (internal change)** — no public API surface change, no new
dependency, no inter-project contract change, and **no observable behaviour change under the
default theme** (byte-identical). The *capability* to diverge is added but not exercised by
default. Per the constitution, Tier 2 requires spec + tests; `.fsi` and baselines remain
untouched (and the drift gates prove they did).

**Gate result**: PASS, no violations → Complexity Tracking left empty. One watch-item: the
resolver lives in `DesignSystem` and is consumed by `Controls` production code, so an
`InternalsVisibleTo FS.GG.UI.Controls` grant is added alongside the test grant. IVT is **not**
public surface — the surface-drift gate is blind to it — so this preserves the zero-surface-delta
invariant (verified in Phase 1 / quickstart V4).

## Project Structure

### Documentation (this feature)

```text
specs/129-central-style-resolver/
├── plan.md              # This file (/speckit-plan output)
├── spec.md              # Feature specification (already present)
├── research.md          # Phase 0 output — placement & seam decisions
├── data-model.md        # Phase 1 output — entities: kind, intent, policy, resolved style
├── quickstart.md        # Phase 1 output — V1–V6 validation runbook
├── contracts/
│   ├── resolver-contract.md          # the front-half resolution path + policy seam surface
│   └── parity-neutrality-contract.md # byte-identity oracle + totality/divergence invariants
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/DesignSystem/
├── Style.fsi                     # UNCHANGED (public back-half resolver `Style.resolve`)
├── Style.fs                      # UNCHANGED (093 resolver + applyVariant/applyCustom)
├── Types.DesignSystem.fsi        # UNCHANGED (ResolvedStyle, Theme, VisualState, StyleClass)
├── StyleResolver.fs              # NEW — `module internal StyleResolver`: front-half path
│                                 #   (kind+intent → baseStyle) + IntentPolicy seam +
│                                 #   neutral default policy; composes Style.resolve. No .fsi.
└── DesignSystem.fsproj           # EDIT — Compile StyleResolver.fs;
                                  #   add InternalsVisibleTo FS.GG.UI.Controls + Controls.Tests

src/Controls/
└── Control.fs                    # EDIT — buttonGeom: replace `primary: bool` with kind+intent,
                                  #   obtain baseStyle from StyleResolver (default policy);
                                  #   faithfulContent: extract intent string, pass kind+intent
                                  #   into the "button"/"icon-button" dispatch.
src/Controls/Widgets/Primitives.fs# UNCHANGED behaviour — intent still lowers to the attribute,
                                  #   but it is now READ by the renderer (no longer dead code).

tests/Controls.Tests/
└── Feature129CentralStyleResolverTests.fs  # NEW — parity (byte-identity), totality
                                            #   (exhaustive cross-product), divergence
                                            #   (non-default policy) tests.
```

**Structure Decision**: Mirror the 126/127 internal-additive pattern. The front-half resolver
lives in **`FS.GG.UI.DesignSystem`** — the central layer that already owns `Theme`,
`ResolvedStyle`, and the back-half `Style.resolve` — as a `module internal` with **no `.fsi`**,
keyed on `kind` (string) and `intent` (string, the existing lowered vocabulary). This keeps it
the single canonical path the master plan names, reuses the back half in the same assembly, and
requires **no new project, no new dependency, and no public-surface change**. Because `Controls`
production code (`buttonGeom`) consumes it, `DesignSystem.fsproj` grants `InternalsVisibleTo` to
both `FS.GG.UI.Controls` and `Controls.Tests`. The default `IntentPolicy` is intent-agnostic
(neutral), so the migration is byte-identical; a non-default policy — supplied directly to the
resolver by the divergence test — proves the seam without editing any control.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
