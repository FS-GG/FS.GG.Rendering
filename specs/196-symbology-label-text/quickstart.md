# Quickstart / Validation: Symbology Label / Glyph Text Channel

A runnable validation guide for the optional identity-label channel. Details live in [contracts/symbology-label-api.md](./contracts/symbology-label-api.md) and [data-model.md](./data-model.md); this file is the run/validate guide, not the implementation.

## Prerequisites

- .NET `net10.0` SDK; repo at `/home/developer/projects/FS.GG.Rendering`.
- Existing packages reused as-is: `FS.GG.UI.Symbology`, `FS.GG.UI.Symbology.Render`, `FS.GG.UI.Scene`, `SkiaViewer` (the render edge that installs the real measurer).

## Build

```bash
cd /home/developer/projects/FS.GG.Rendering
dotnet build FS.GG.Rendering.slnx -c Debug
# or just the affected projects:
dotnet build src/Symbology/Symbology.fsproj -c Debug
dotnet build src/Symbology.Render/Symbology.Render.fsproj -c Debug
```

## FSI smoke (run first — confirms the surface is usable)

After `Symbology.fsi` carries `Label` and a first `.fs` stub exists, exercise the public surface before building out the stories:

```fsharp
open FS.GG.UI.Symbology
open FS.GG.UI.Scene

let labelled = { Symbology.defaultToken with Label = Some "A-7" }
let bare     = Symbology.defaultToken          // Label = None

// label renders in every grammar (C-01)
Symbology.token labelled |> ignore
Symbology.badge labelled |> ignore
Symbology.ring  labelled |> ignore
Symbology.render Grammar.Badge labelled |> ignore

// opt-in zero drift: bare token byte-identical to pre-feature (C-02)
let bytes s = (SceneCodec.export s).CanonicalBytes
(bytes (Symbology.token bare)) = (bytes (Symbology.token Symbology.defaultToken))  // true

// empty/whitespace => no label, no throw (C-06)
Symbology.token { Symbology.defaultToken with Label = Some "   " } |> ignore

// degenerate + label => placeholder, no throw (C-07)
Symbology.badge { Symbology.defaultToken with R = 0.0; Label = Some "X" } |> ignore

// label observably alters output (C-03)
(bytes (Symbology.token labelled)) <> (bytes (Symbology.token bare))               // true
```

## Run the tests

```bash
# core scene-construction batteries (presence/determinism/placeholder/gallery/legibility + new LabelTests)
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj

# render-bridge tofu test (rasterises a labelled token under the installed real measurer)
dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj

# board reproducibility
dotnet test tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj

# full baseline across all test projects (optional)
dotnet fsi scripts/baseline-tests.fsx --out readiness/196-symbology-label-text/baseline.md
```

## Validate against Success Criteria

| SC | Check | How |
|---|---|---|
| **SC-001** | One mapping → all three grammars | Render `Some "A-7"` via `token`/`badge`/`ring`/`render g`; label appears in each, no per-grammar mapping. |
| **SC-002** | Tofu-free, distinguishable labels | `Symbology.Render.Tests`: rasterise a labelled token through `Render.toPng`; assert the label glyph run is non-tofu (`Missing = false`); a roster of distinct labels is mutually distinguishable. |
| **SC-003** | `Label = None` byte-identical | `DeterminismTests`: `token`/`gallery`/`filmstrip` golden SHAs **unchanged**; bare token bytes equal pre-feature bytes. |
| **SC-004** | In-proc & cross-proc byte-identity (fixed provider) | `DeterminismTests`: render labelled token twice, `Expect.equal` canonical bytes; pinned golden SHA as the cross-process proxy. |
| **SC-005** | Fit / empty / degenerate safe | `LabelTests`: overlong fitted label measures ≤ region width (no mid-glyph cut, no overflow); empty/whitespace ⇒ no label; `PlaceholderTests`: `R <= 0` + label ⇒ placeholder; **zero** exceptions throughout. |
| **SC-006** | Linter grammar-independent & unchanged | `LegibilityTests`: a fixed roster's `Report` is identical with/without labels and across all three grammars; capacity table untouched. |
| **SC-007** | Baseline moves only for Symbology; skill documents label | surface-drift check shows only `FS.GG.UI.Symbology.txt` changed (gains `Label`); skill-parity passes critical=0/high=0. |

## Surface baseline refresh (Tier 1)

```bash
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff readiness/surface-baselines/
# EXPECT: only FS.GG.UI.Symbology.txt changes — the Token record gains the `Label` field.
# Every other baseline (Scene / SkiaViewer / Controls / Canvas / Legibility / Symbology.Render) = zero drift.
```

## Skill parity (FR-015)

```bash
# After editing the canonical src/Symbology/skill/SKILL.md (label section) and mirroring to
# template/product-skills/fs-gg-symbology/SKILL.md (.claude/ and .agents/ inherit via pointer wrappers):
dotnet fsi scripts/check-agent-skill-parity.fsx
# EXPECT: critical=0, high=0. The skill documents the label as an opt-in inspection-detail identity
# channel (requires the real measurer for tofu-free output; keep strings short; complements the sigil).
```

## Done when

- FSI smoke + all extended/new tests green; `token`/`gallery`/`filmstrip` goldens unchanged.
- Render-bridge tofu test green (label non-tofu under the installed measurer).
- Surface diff is exactly the `Label` field on `Token`, zero drift elsewhere.
- Skill-parity passes; the label section is documented canonically and mirrored.
