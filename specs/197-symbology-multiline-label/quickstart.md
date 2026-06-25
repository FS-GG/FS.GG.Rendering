# Quickstart — Symbology Multi-line / Paragraph Label Channel

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Contract**: [contracts/symbology-multiline-label-api.md](./contracts/symbology-multiline-label-api.md)

A build + FSI-smoke + test + per-SC validation guide. Implementation detail lives in `tasks.md` and the
source; this is the run/validation guide.

## Prerequisites

- .NET `net10.0` SDK; repo builds via `dotnet build FS.GG.Rendering.slnx -c Debug`.
- Feature branch `197-symbology-multiline-label` checked out.
- No `.fsi` edit, no baseline regeneration, no new font files are expected (FR-013/FR-014/FR-016).

## 1. Build

```sh
dotnet build FS.GG.Rendering.slnx -c Debug
```

Expect 0 warnings / 0 errors. The only edited source file is `src/Symbology/Symbology.fs` (+ tests + skill).

## 2. Early FSI smoke (Foundational — do this first, before US work)

Load the public surface and confirm the layered zero-drift anchor + multi-line stacking by construction
(values illustrative — assert in tests, not by eye):

```fsharp
#load "src/Symbology/Symbology.fs"   // via the usual FSI harness / script the repo uses
open FS.GG.UI.Symbology
open FS.GG.UI.Scene

let t = { Symbology.defaultToken with Cx = 60.0; Cy = 60.0; R = 24.0 }

// C1: no label == pre-feature symbol
(SceneCodec.export (Symbology.token { t with Label = None })).CanonicalBytes
  = (SceneCodec.export (Symbology.token Symbology.defaultToken |> ignore; Symbology.token { t with Label = None })).CanonicalBytes

// C2: one fitting line == spec-196 single-line render (byte-identical)
let oneLine   = (SceneCodec.export (Symbology.token { t with Label = Some "HMR-7" })).CanonicalBytes
// C3/C4: \n and wide-whitespace labels add MORE glyph-run nodes (stacked), but never throw
let multi     = Symbology.token { t with Label = Some "HAMMER\nA-7" }
let wide      = Symbology.token { t with Label = Some "ALPHA BRAVO CHARLIE DELTA ECHO" }
// C11: degenerate + label => placeholder, no throw
let degenerate = Symbology.token { t with R = 0.0; Label = Some "X\nY" }
```

Smoke gate (assert in `MultilineLabelTests`): `Label = None` and one-line `"HMR-7"` are byte-identical to
their spec-196 renders; a `\n` label emits N stacked nodes with the first at the 196 baseline; a wide
whitespace label wraps; an over-budget label caps + ends `…`; the degenerate labelled token returns the
placeholder without throwing.

## 3. Run the tests

```sh
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj -c Debug
dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj -c Debug
```

Expect: all existing assertions still green **unmodified** (the `0dda10bd…`/`6710215b…`/gallery/filmstrip/
badge/ring goldens and the empty/whitespace/no-whitespace-overlong cases), plus the new multi-line battery
and the multi-line render-bridge tofu test passing.

## 4. Per-Success-Criterion validation

| SC | How to validate | Where |
|---|---|---|
| SC-001 | A multi-line label renders in all three grammars with no per-grammar mapping and no board-signature change | `MultilineLabelTests`, `GalleryTests` |
| SC-002 | Every line tofu-free (`TofuCount = 0`) through `Render.toPng`; distinct labels ⇒ distinct output | `Symbology.Render.Tests` (multi-line case) |
| SC-003 | `Label = None` and one-line `"HMR-7"` byte-identical to spec-196 goldens (`0dda10bd…`, `6710215b…`) | `DeterminismTests` (unchanged goldens) |
| SC-004 | Same multi-line `Token` rendered twice byte-equal + a new pinned multi-line golden | `DeterminismTests` |
| SC-005 | Each drawn line ≤ region width; line count ≤ budget; empty/whitespace & degenerate ⇒ 0 exceptions | `MultilineLabelTests`, `PlaceholderTests` |
| SC-006 | `Legibility.score` `Report` identical with vs without labels; grammar-independent | `LegibilityTests` |
| SC-007 | No baseline diff anywhere; skill parity 0 critical/0 high | `git diff readiness/surface-baselines`, `scripts/check-agent-skill-parity.fsx` |

## 5. Surface baseline check (expect NO change)

```sh
git status --porcelain readiness/surface-baselines/
```

Expect **empty output** — multi-line reuses the existing `Label : string option`, so no public surface is
added (FR-013). If anything appears here, a surface leak slipped in; investigate before continuing.

## 6. Skill parity

```sh
dotnet fsi scripts/check-agent-skill-parity.fsx
```

Expect `critical=0 high=0` after the multi-line section is authored in `src/Symbology/skill/SKILL.md` and
mirrored to `template/product-skills/fs-gg-symbology/SKILL.md`.
