# Quickstart / Validation Guide: Symbology Full Rich-Text Layout

**Feature**: 199-rich-text-layout | **Date**: 2026-06-26

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

Once the new `LabelRun` fields, `LabelAlign`/`LabelParagraph`/`LabelText.Laid`, the decoration/slant/tracking
emission in `richLabelNodes`, and `laidLabelNodes` exist, confirm the channel behaves before building out the
stories. Through the **public** surface (`Symbology.token`/`render` + `SceneCodec`):

1. **Layered zero-drift**: `Some (LabelText.Plain "HMR-7")` canonical bytes still equal the pinned spec-197
   golden and `Label = None` equals the pre-feature golden; `Some (LabelText.Rich [ Symbology.run "HMR-7" ])`
   (all-default, incl. the new attrs) is **byte-identical** to that `Plain` (B3); and
   `Some (laidLabel [ paragraph [ Symbology.run "HMR-7" ] ])` (single `Center` paragraph, all-default) is
   **byte-identical** to that `Rich`/`Plain` (B4 — default alignment = 198 flow).
2. **Per-run typography is a channel**: a run with `Italic = Some true` (or `Underline`/`Strike`/`Tracking`)
   produces **different** bytes from the same characters without it, and neither raises (B5/B6).
3. **Alignment places lines**: the same wrapping content under `Leading` / `Trailing` differs from `Center`,
   and `Justify` over content that wraps to ≥2 lines fills each wrapped line while leaving the **last** line
   of the paragraph un-justified (B7/B8).
4. **Fit**: an over-budget laid-out label caps to the grammar budget with a trailing ellipsis on the last
   drawn line under every alignment; a tracked run's spacing widens its measured width (B10/B12).
5. **Safe**: `Laid []` / all-whitespace paragraphs ⇒ no label node, no throw (B13); a degenerate `R = 0.0`
   laid-out/decorated token ⇒ placeholder, no throw (B14).

Treat this smoke (and the render-bridge tofu test below) — not the plan narrative — as confirmation the
channel works.

## Run the tests

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj
dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj
```

Expected: existing batteries stay green (the new `LabelRun` fields are additive `None` defaults; default
`Center`/single-paragraph reduces to the 198 path); the extended `RichLabelTests.fs` typography battery, the
new `LaidLabelTests.fs` paragraph-layout battery, and the laid-out/decorated render-bridge tofu test pass.

## Per-Success-Criterion validation

| SC | How to validate | Evidence |
|---|---|---|
| **SC-001** | A roster maps `'stats -> Token` with `Laid` (aligned/decorated) labels; `render`/`galleryIn` draw them in all 3 grammars with no signature change | gallery scenes per grammar |
| **SC-002** | Render-bridge raster of a laid-out/decorated label: every run `TofuCount = 0`; two same-char labels differing in alignment or decoration produce observably distinct output | `Symbology.Render.Tests` tofu + presence assertions |
| **SC-003** | `None` ≡ pre-feature golden; `Plain` ≡ spec-197 golden; all-default `Rich` ≡ `Plain`; single `Center` all-default `Laid` paragraph ≡ that `Rich`/`Plain` (default = 198 flow) | pinned 198/197/196/pre-feature byte-equal + B3/B4 |
| **SC-004** | Same `Laid` token in-process and cross-process under a fixed provider ⇒ byte-identical (incl. justified lines) | new pinned laid-out cross-process golden |
| **SC-005** | Over-wide/over-tall/over-numerous/justified/decorated label: no clip, no overflow, capped lines, max-height lines, decoration ≤ fitted extent, last paragraph line un-justified; empty-paragraph & degenerate-with-label ⇒ no exception | `LaidLabelTests` + `RichLabelTests` fit/justify/decoration/empty/placeholder cases |
| **SC-006** | `Legibility.score` identical across grammars and unchanged by laid-out/decorated-label presence | linter-invariance assertion |
| **SC-007** | Only the symbology surface baseline moves (regenerated); all others unchanged; skill parity `critical=0 high=0` | `readiness/surface-baselines/` diff + parity report |

## Surface baseline regeneration (FR-017)

The symbology `.fsi` gains the four `LabelRun` fields, `LabelAlign`, `LabelParagraph`, the `LabelText.Laid`
case, and the constructors — so the symbology surface baseline **moves** and MUST be regenerated; confirm
**zero drift** on every other package baseline:

```bash
# regenerate per the repo's surface-baseline workflow, then verify only FS.GG.UI.Symbology.* changed
git status -- readiness/surface-baselines/
```

## Skill parity (FR-020)

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx   # expect critical=0 high=0
```

Confirms the full-rich-text section is authored in `src/Symbology/skill/SKILL.md` and mirrored to
`template/product-skills/fs-gg-symbology/SKILL.md`.
