# Quickstart / Validation Guide: Symbology Rich-Text Label Runs

**Feature**: 198-symbology-rich-text-label | **Date**: 2026-06-25

How to build, smoke, and validate the feature end-to-end. Run from the repo root. Implementation detail
lives in `tasks.md` / the source; this is a **run/validation** guide.

## Prerequisites

- .NET `net10.0` SDK; the repo builds via `FS.GG.Rendering.slnx`.
- Projects exercised: `src/Symbology`, `src/Symbology.Render`, `tests/Symbology.Tests`,
  `tests/Symbology.Render.Tests`.

## Build

```bash
dotnet build src/Symbology/Symbology.fsproj
dotnet build tests/Symbology.Tests/Symbology.Tests.fsproj
dotnet build tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj
```

## Early FSI smoke (Foundational — before US1/US2/US3)

Once `LabelRun`/`LabelText` and `richLabelNodes` exist, confirm the channel behaves before building out
the stories. Through the **public** surface (`Symbology.token`/`render` + `SceneCodec`):

1. **Zero-drift — Plain ≡ 197**: `SceneCodec.export (token { defaultToken with Label = Some (LabelText.Plain "HMR-7") })` canonical bytes **equal** the pinned spec-197 `6710215b…` golden, and `Label = None` equals `0dda10bd…`.
2. **All-default Rich ≡ Plain**: `Some (LabelText.Rich [ Symbology.run "HMR-7" ])` is **byte-identical** to `Some (LabelText.Plain "HMR-7")` (FR-002 / B3).
3. **Styling is a channel**: a two-run styled label (e.g. `run "BRAVO-6"` bold/blue + `run " ac-12"` dim/small) emits **≥2** glyph-run nodes (`Scene.describe`) and differs in bytes from the same characters as `Plain` (B4/B5).
4. **Fit**: an over-wide styled run wraps/shrinks within the region; an over-budget styled label caps to the grammar budget with a trailing ellipsis (B6).
5. **Safe**: `Rich []` / all-whitespace runs ⇒ no label node, no throw (B8); a degenerate `R = 0.0` styled token ⇒ placeholder, no throw (B9).

Treat this smoke (and the render-bridge tofu test below) — not the plan narrative — as confirmation the
channel works.

## Run the tests

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj
dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj
```

Expected: existing batteries stay green after the **value-preserving** fixture migration
(`Some "X"` → `Some (LabelText.Plain "X")`); the new `RichLabelTests.fs` battery and the styled
render-bridge tofu test pass.

## Per-Success-Criterion validation

| SC | How to validate | Evidence |
|---|---|---|
| **SC-001** | A roster maps `'stats -> Token` with `Rich` labels; `render`/`galleryIn` draw them in all 3 grammars with no signature change | gallery scenes per grammar |
| **SC-002** | Render-bridge raster of a styled label: every run `TofuCount = 0`; two same-char/different-style labels differ in output | `Symbology.Render.Tests` tofu + presence assertions |
| **SC-003** | `None` ≡ pre-feature golden; `Plain` (1- & multi-line) ≡ spec-197 goldens; single default `Rich` run ≡ `Plain` | `0dda10bd…` / `6710215b…` / `b41c9626…` byte-equal + B3 |
| **SC-004** | Same `Rich` token in-process and cross-process under a fixed provider ⇒ byte-identical | new pinned styled cross-process golden |
| **SC-005** | Over-wide/over-tall/over-numerous styled label: no clip, no overflow, capped lines, max-height lines; empty-run & degenerate-with-label ⇒ no exception | `RichLabelTests` fit/cap/empty/placeholder cases |
| **SC-006** | `Legibility.score` identical across grammars and unchanged by styled-label presence | linter-invariance assertion |
| **SC-007** | Only the symbology surface baseline moves (regenerated); all others unchanged; skill parity `critical=0 high=0` | `readiness/surface-baselines/` diff + parity report |

## Surface baseline regeneration (FR-015)

The symbology `.fsi` gains `LabelRun`, `LabelText`, the retyped `Token.Label`, and the constructors —
so the symbology surface baseline **moves** and MUST be regenerated; confirm **zero drift** on every
other package baseline:

```bash
# regenerate per the repo's surface-baseline workflow, then verify only FS.GG.UI.Symbology.* changed
git status -- readiness/surface-baselines/
```

## Skill parity (FR-017)

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx   # expect critical=0 high=0
```

Confirms the rich-text section is authored in `src/Symbology/skill/SKILL.md` and mirrored to
`template/product-skills/fs-gg-symbology/SKILL.md`.
