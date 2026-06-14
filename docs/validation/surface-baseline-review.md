# Public surface review — full surface-baseline coverage

> Review opened **and resolved** 2026-06-14 from the R6 CI-wiring finding (see
> [`docs/ci/cadence-map.md`](../ci/cadence-map.md) §4). The gate's surface-drift check originally
> guarded **4 of 9** committed baselines because `scripts/refresh-surface-baselines.fsx` only
> regenerated those 4. Extending it surfaced that the other 5 committed baselines were **stale stubs**
> capturing a fraction of the real public surface — so "covering" them was treated as a **Principle II
> surface decision** (visibility lives in `.fsi`), reviewed **before** re-baselining.
>
> ## Conclusion (resolved)
>
> **No over-exposure found — the 5 surfaces were simply stale, not wrong.** Every public type in the
> drifting packages is either declared in a curated `.fsi` or explicitly `internal`:
> - **Scene, Testing, Elmish** — every `.fs` has an `.fsi` companion; the surfaces are fully governed.
> - **SkiaViewer** — the one file without an `.fsi` (`SceneRenderer.fs`) is `module internal` and
>   exposes nothing. The flagged `EvidenceWorkflow*` / `GeneratedAppHost*` families are **deliberate
>   public API**: `GeneratedAppHost` is the documented seam `Controls.Elmish` adapts onto
>   (`SkiaViewer.fsi` D3-AMEND), `EvidenceWorkflow*` is the pure MVU core tested by `SkiaViewer.Tests`,
>   and the template ships a curated `api-surface/SkiaViewer/SkiaViewer.fsi` snapshot including them.
>
> **Action taken:** generator extended to all 9 + anon-type filter, writing to the committed
> location; all 9 re-baselined (purely additive: **365 net-new public types, 0 removed**); the gate
> now drift-checks the full surface via regenerate-then-`git diff`. No `.fsi` tightening was needed.
>
> The detail below is retained as the record of what was examined.

## How the candidate surfaces were generated

A corrected generator (all 9 packages; loads each assembly by path with a cross-assembly resolve
handler; **excludes compiler-generated/anonymous types** via `CompilerGeneratedAttribute` — their
names embed a non-deterministic hash, e.g. `<>f__AnonymousType2574703318\`6`, and must never enter a
baseline). Format otherwise identical to the existing generator (`GetExportedTypes`, `Module`-suffix
stripped, distinct, sorted). The live generator/gate are **unchanged** for now so the gate stays
green; they change only once the surface below is settled.

## Status per baseline

| Package | Committed baseline | Current public surface (anon-filtered) | Drift | Action |
|---|---|---|---|---|
| Layout | 32 | 32 | none | ✅ already guarded — no action |
| KeyboardInput | — | match | none | ✅ already guarded — no action |
| Controls | — | match | none | ✅ already guarded — no action |
| Controls.Elmish | — | match | none | ✅ already guarded — no action |
| Input | (matches) | match | none | ✅ matches — fold into the generator as-is |
| **Elmish** | **4** | **12** | +8 | review: 8 newly-exposed types (no evidence/diagnostic flavor) |
| **Scene** | **8** | **133** | +125 | review: **18** evidence/diagnostic/workflow-flavored among them |
| **SkiaViewer** | **5** | **183** | +178 | review: **53** evidence/diagnostic/workflow-flavored among them |
| **Testing** | **3** | **57** | +54 | review: **21** evidence/diagnostic-flavored — but plausibly *intended* (test-helper package) |

The committed stubs are not real drift guards — Scene captures 8 of ~133 actual public types. They
were last refreshed long before the current source landed.

## The decision (per package)

For each drifting package, the real question is **not** "snapshot whatever is public today" but
"**what is *meant* to be public?**" The large `Evidence*` / `Diagnostic*` / `Workflow*` /
`Readback*` / `Replay*` families look like internal diagnostics that leaked through missing `.fsi`
restrictions. Blindly baselining them would bless the leak.

Suggested read (yours to confirm):

- **Elmish (+8):** small; quick eyeball. Likely a mix of genuine API (`ElmishAdapter*`) and a few
  `Animation*` additions. Probably keep most; baseline once confirmed.
- **Scene (+125, 18 diagnostic):** the geometry/scene-graph API (`Color`, `Rect`, `Clip`, `BlendMode`,
  `SceneNode+…`) is genuine public API and should be baselined. The `*Evidence` / `*Diagnostic`
  families (`SceneEvidence*`, `LayoutEvidence*`, `RenderDiagnostic`, …) are candidates to
  **internalize** (move to `.fsi`-hidden or an internal module) unless a consumer needs them.
- **SkiaViewer (+178, 53 diagnostic):** biggest concern. The core `Viewer*` API is genuine; the
  `EvidenceWorkflow*` (Effect/Model/Msg + all DU cases), `GeneratedAppHost*`, and `Host.Diagnostic*`
  families are very likely internal harness/diagnostic plumbing that should **not** be public surface.
  Strong candidate for `.fsi` tightening before baselining.
- **Testing (+54, 21 evidence):** this package *exists* to expose testing/evidence helpers, so its
  `Evidence*` / `GeneratedProduct*` / `Host*` types are **plausibly intended public API**. Likely
  baseline most of it as-is, after a sanity pass.

## Recommended workflow to finish (after the review)

1. For each flagged family, decide **public vs internal**; tighten the owning `.fsi` where
   over-exposed (Principle II). This is the substantive work and should be reviewed.
2. Once the surfaces are settled, land together (so the gate never goes red mid-flight):
   - swap `scripts/refresh-surface-baselines.fsx` to the corrected all-9 + anon-filter generator;
   - regenerate and commit all 9 `tests/surface-baselines/*.txt`;
   - point the gate's drift step at all 9 (no `gate.yml` change needed — it already diffs every
     generated file) and **remove the 4-of-9 caveat** from `cadence-map.md` §4.
3. The gate then guards the full, *intended* public surface — green means what it says.

> Until step 2 lands, the gate honestly guards 4 of 9 and discloses the rest (cadence-map §4). No
> overclaim in the interim.
