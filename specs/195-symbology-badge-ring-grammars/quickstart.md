# Quickstart: Badge & Ring Alternative Symbology Grammars

Validation/run guide for the two new pure grammars in `FS.GG.UI.Symbology`. Implementation detail lives in
[data-model.md](./data-model.md) and [contracts/symbology-grammars-api.md](./contracts/symbology-grammars-api.md);
this is the runnable proof the feature works end-to-end. All steps are headless (no GL, no window system).

## Prerequisites

- .NET `net10.0` SDK; repo restores cleanly.
- Working dir: repo root `/home/developer/projects/FS.GG.Rendering`.
- No new dependency, no new project: the surface lands in the existing `src/Symbology/Symbology.fsproj`.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Debug
```

Expected: clean build, 0 warnings. (Pre-existing reds in `tests/Package.Tests` and
`samples/ControlsGallery/ControlsGallery.Tests` are NOT regressions — see `readiness/baseline.md`.)

## 2. FSI smoke (run BEFORE building out US1/US2 — the Foundational confirmation)

Once `Symbology.fsi` carries `type Grammar` + the new `val`s and a first `.fs` stub exists, exercise the public
surface (the contract's FSI smoke):

```fsharp
open FS.GG.UI.Symbology
let t = Symbology.defaultToken
Symbology.badge t |> ignore
Symbology.ring  t |> ignore
Symbology.render Grammar.Badge t |> ignore
Symbology.render Grammar.Ring  t |> ignore
Symbology.badge { t with R = 0.0 } |> ignore   // placeholder, no throw
```

Expected: each call returns a non-empty `Scene`; the degenerate call returns the placeholder; nothing throws.
Treat **this smoke**, not the plan narrative, as the confirmation the surface is usable.

## 3. Run the tests

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj
dotnet test tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj
```

Expected: all green; new/extended tests pass; **no prior assertion weakened**.

## 4. Per-Success-Criterion validation

| SC | What it asserts | How to validate |
|---|---|---|
| **SC-001** | One mapping → all three grammars, zero ChannelMap change | A roster built once renders via `render Grammar.{Token,Badge,Ring}`; same `Token`s, three scenes. |
| **SC-002** | Every channel observable in Badge & Ring | ChannelPresence battery: vary ONE channel, assert canonical bytes change (per channel, per new grammar). |
| **SC-003** | Byte-identical re-render (in-proc & cross-proc) | Determinism battery: render same `Token` twice in `badge`/`ring`, compare **canonical SceneCodec bytes**. |
| **SC-004** | Degenerate token → visible placeholder, zero exceptions | Placeholder battery: `R <= 0` in `badge`/`ring` ⇒ non-empty placeholder scene, no throw. |
| **SC-005** | Linter report identical across grammars | Render roster in each grammar (scenes differ) yet `Legibility.score roster` returns the **same** `Report`. |
| **SC-006** | Token grammar zero drift; only symbology baseline moves | Token golden bytes pinned & unchanged; surface-drift check shows only `FS.GG.UI.Symbology.txt` moved. |
| **SC-007** | Skill documents Badge/Ring; parity passes | `scripts/check-agent-skill-parity.fsx` → critical=0, high=0. |
| **FR-007** | Ring health arc monotone in health | Ring health-monotonicity test: sweep grows non-decreasing across `Health` ∈ [0,1]. |

## 5. Surface-baseline refresh (Tier 1)

Regenerate the symbology surface baseline and confirm the delta is exactly the new `Grammar` type:

```bash
# regenerate per repo tooling, then diff
git diff readiness/surface-baselines/
```

Expected diff — **only** in `FS.GG.UI.Symbology.txt`:

```diff
+ FS.GG.UI.Symbology.Grammar
+ FS.GG.UI.Symbology.Grammar+Tags
```

Every other baseline (`Scene`, `SkiaViewer`, `Controls`, `Canvas`, `Legibility`, …) must show **zero** drift.

## 6. Skill parity (FR-013/SC-007)

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx
```

Expected: passes with critical=0, high=0 after the grammar section is updated canonically in
`src/Symbology/skill/SKILL.md` and mirrored to `template/product-skills/fs-gg-symbology/SKILL.md`.

## 7. (Optional, US3/P3) Grammar-compare board

If the optional `samples/SymbologyBoard` grammar-compare demo is built, render the same roster as a gallery in
each grammar and confirm each board is byte-reproducible — the A/B surface for picking the form factor that
reads best at the target on-board size. This sample is a demonstration, not a contract.
