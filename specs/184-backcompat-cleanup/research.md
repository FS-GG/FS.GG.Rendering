# Phase 0 Research: Backward-Compatibility Shim Removal

All four removal candidates were located at HEAD by parallel Explore passes; the four decisions below
resolve the open questions the spec left implicit (true tier per item, what the surface oracle actually
captures, how to migrate the one US2 production caller byte-stably, and the US4 descope verdict).

---

## D1 — Per-item change classification (refines the spec's blanket "Tier 1")

**Decision.** Classify per item by whether it changes the **public** surface:

| Item | Where it lives | Public? | Tier | Bump + ledger? |
|---|---|---|---|---|
| US1 `ScrollViewport.MaxOffset` | `Control.fsi:283` (public type's record field) | **Yes** | **Tier 1** | **Yes** — `FS.GG.UI.Controls` |
| US2 `Composition` legacy layer | `Composition.fsi:9` → `module **internal** Composition` | **No** | **Tier 2** | No |
| US3 `ControlEvent.Payload` | `Types.fsi:312-322` (public type's record field) | **Yes** | **Tier 1** | **Yes** — `FS.GG.UI.Controls` |
| US4 flat-chart fallback | internal branch in `chartValues` (`Control.fs:482-483`) | **No** | **Tier 2** | No |

**Rationale.** The constitution defines Tier 1 as a change to *public* API surface. `Composition` is
declared `module internal` (`Composition.fsi:9` — "This module is assembly-internal"), so `LegacyForm`,
`LegacyCompatibilityStatus`, `legacyLower`, and `compatibilityEvidence` are **never on the public
surface** and do not appear in any `readiness/surface-baselines/*.txt` (verified: `grep -i legacy
FS.GG.UI.Controls.txt` returns nothing). The chart fallback is an internal match arm inside the
`chartValues` helper, not a public signature. Removing internal code is Tier 2 — it still requires
tests + byte-stable behavior, but **no `.fsi`-public change, no package bump, no CompatibilityLedger
entry** per the constitution. US1 and US3 *do* remove a public record field from a public type, so they
are genuinely Tier 1.

**Alternatives considered.** *(a) Honor the spec's blanket Tier-1 and bump for all four.* Rejected:
bumping a package for a purely internal change misrepresents the release (a consumer diffing the
public surface sees no change) and contradicts the constitution's Tier definition. *(b) Treat US2/US4
as Tier 1 "because they touch a production path."* Rejected: production-path-touching drives the
**byte-stability** gate (FR-005), not the **surface/bump** gate (FR-006/007) — those are orthogonal.
**Maintainer awareness:** flagged in plan.md; if a conservative bump is preferred for US2/US4 it is a
one-line `.fsproj` edit in Polish, but it is not required.

---

## D2 — What the surface baseline `.txt` actually captures (it's type-granular)

**Decision.** Treat the **`.fsi` diff** as the binding surface evidence for US1/US3; expect the
coarse baseline `.txt` to be **unchanged**, and regenerate it only to *confirm* no unintended type
moved.

**Rationale.** `scripts/refresh-surface-baselines.fsx` builds each baseline from
`Assembly.GetExportedTypes()` → type `FullName`s (plus DU `+Case` tags), filtered of
compiler-generated names, sorted (lines 51-72). It enumerates **types and DU cases, not record
fields**. `ScrollViewport` and `ControlEvent` remain exported types after their field removals, so
`FS.GG.UI.Controls.txt` does not change. The authoritative public-surface declaration is the `.fsi`
(Principle II); removing `MaxOffset: float` / `Payload: string option` from the `.fsi` *is* the public
change, captured by `git diff src/Controls/Control.fsi src/Controls/Types.fsi`.

**Consequence for FR-006.** "Update the affected surface baseline" is satisfied by: regenerate →
confirm `git diff readiness/surface-baselines/` is empty (no type added/removed) → record the
`.fsi` diff as the surface delta in the ledger. A non-empty baseline diff would mean an *unintended*
type-level change and blocks the story.

**Alternatives considered.** *(a) Make the baseline field-granular.* Out of scope — changing the
oracle is its own feature; the `.fsi` already enforces field visibility. *(b) Assume no surface change
at all and skip the bump.* Rejected for US1/US3 — the `.fsi` is the surface, and removing a public
field from it is a breaking public change requiring a bump + ledger even though the `.txt` is silent.

---

## D3 — US2 byte-stable overlay migration + `ModifierSource` retention

**Decision.** Replace the one production call
`Composition.legacyLower Composition.LegacyOverlay` (`Control.fs:2400`) with the **literal entry it
produces**:

```fsharp
[ { Composition.Source = Composition.LegacyOverlaySource
    Composition.Effect = Composition.LayerHint "overlay" } ]
```

Then delete `LegacyForm`, `LegacyCompatibilityStatus`, `legacyLower`, and `compatibilityEvidence`.
**Retain `ModifierSource.LegacyOverlaySource`** (and, conservatively, the other `ModifierSource.Legacy*Source`
cases) as FR-010 live-despite-name identities.

**Rationale.** `legacyLower LegacyOverlay` evaluates to exactly
`[{ Source = LegacyOverlaySource; Effect = LayerHint "overlay" }]` (`Composition.fs:389`). Emitting
that literal yields a byte-identical `ModifierEntry list`, so `Composition.normalize` /
`Composition.fingerprint` / `applyChain` produce identical output — the overlay paint/hit/z-order and
the scene fingerprint are unchanged (FR-005). `LegacyOverlaySource` is a case of `ModifierSource`
(the **modern** modifier IR's provenance enum, `Composition.fsi:28-36`), **not** part of the
`LegacyForm` layer being removed; it is load-bearing for the overlay fingerprint and MUST stay. The
other `Legacy*Source` cases become unreferenced once `legacyLower` is gone but pruning them is a
separate type-DU edit with no behavior payoff — retain them (or prune in a follow-up) rather than
expand US2's blast radius. `compatibilityEvidence` is only consulted by the Feature-140 legacy tests
(which are deleted), so it has no remaining caller.

**Alternatives considered.** *(a) Map overlay to a non-legacy `Source` (e.g. `AuthoredModifier`).*
Rejected — changes the fingerprint input → not byte-stable (violates FR-005). *(b) Keep `legacyLower`
but inline only the overlay arm.* Rejected — leaves the dead layer half-removed (fails SC-001 "zero
ambiguous kept-but-unused"). *(c) Prune all `Legacy*Source` cases now.* Deferred — unnecessary scope;
recorded as an optional follow-up.

---

## D4 — US4 descope verdict: removal is in-scope (zero flat-list authors)

**Decision.** Remove the flat-chart fallback. FR-004's removal condition ("only if no in-tree consumer
authors flat float-list chart data") is **met**.

**Rationale.** An exhaustive scan of `src/`, all 4 samples, and the template found **zero** call sites
authoring `float list`/`float array` chart data. Every chart is authored through the typed front door
— `LineChart.series`/`BarChart.series`/`PieChart.values` with `ChartSeries`/`ChartPoint` lists
(samples: `SecondAntShowcase`, `ControlsGallery`, `AntShowcase` `Pages.fs:281-283/206-208`; template:
`View.fs:81`, `BehaviorTests.fs:112`). The only exercise of the fallback is one deliberate test,
`Feature080ExtractionTests.fs:62-71` ("flat float-list fallback still extracts (legacy authoring)"),
which is deleted with the behavior (FR-008). Removing arms `Control.fs:482-483` leaves the typed arms
`479-481` untouched → typed `chartValues` output byte-identical (FR-005).

**Alternatives considered.** *(a) Keep the fallback "just in case" an external author uses it.*
Rejected by the spec premise (no external consumer; in-tree verified clean) and SC-001 (no
kept-but-unused). *(b) Replace the fallback with a typed adapter.* Unnecessary — no caller needs it.
**Descope trigger remains armed:** if `/speckit-tasks`' re-scan (Foundational) finds any flat-list
author, US4 is dropped and the finding recorded (FR-004 / Acceptance 4.3), no consumer broken.

---

## Cross-cutting findings

- **Single package.** All four items live in `FS.GG.UI.Controls` (`src/Controls/`,
  `0.1.45-preview.1`). The only cross-package coupling is US3's dual-set **writers** in
  `FS.GG.UI.Controls.Elmish` (`0.1.46-preview.1`, `ControlsElmish.fs`) — internal functions (no
  `Payload` in `Controls.Elmish.*.fsi`), so removing the field forces a recompile + re-pin, **not** an
  Elmish public-surface change or bump.
- **One Controls bump covers US1 + US3** when they land together: `0.1.45-preview.1 → 0.1.46-preview.1`.
- **Baseline red/green to reproduce exactly (FR-011 / SC-004):** `Package.Tests` 8-fail +
  `ControlsGallery` 2-fail (stale-feed, pre-existing per `specs/182|183/.../known-reds.md`); 14 other
  `*.Tests.fsproj` green. No new red, no flipped green.
- **No wire format touched.** `SceneCodec` is untouched; US2 edits only the in-memory `Composition`
  modifier IR. No persisted/replay artifact is at risk.
