# Quickstart: Symbology Auto-Label & Label-Bound Motion

Validation/run guide for feature 200. Proves auto-derived labels and label-bound motion end-to-end against the success criteria. References [contracts/symbology-auto-label-motion-api.md](./contracts/symbology-auto-label-motion-api.md) and [data-model.md](./data-model.md) rather than duplicating them. Implementation belongs in `tasks.md` / the implementation phase.

## Prerequisites

- .NET `net10.0` SDK; the `FS.GG.Rendering.slnx` solution restores.
- Changes land in the existing `src/Symbology/` package (`Symbology.fsi` first, then `Symbology.fs`) and the existing `tests/Symbology.Tests/` + `tests/Symbology.Render.Tests/` projects. No new project.

## Build

```bash
dotnet build src/Symbology/Symbology.fsproj
```

## FSI smoke (Foundational — run BEFORE building out US1/US2/US3)

Load the public surface and confirm the layered contract by hand (mirrors the plan's standing-assumption smoke). Conceptual checks (exact harness per `tests/`):

1. **Opt-out ≡ 199.** `defaultToken` (with `AutoLabel = None`, `LabelMotion = None`) renders byte-identically to the spec-199 token — `SceneCodec.export(token t).CanonicalBytes` equals the pinned 199 golden.
2. **Auto-label projects from channels.** `{ defaultToken with R = 40.0; Faction = Enemy; Health = 0.87; AutoLabel = Some (Symbology.autoLabel [FactionCode; HealthTier]) }` draws a label (e.g. `ENY H87`) in each grammar via `render`.
3. **Explicit overrides auto.** Adding `Label = Some (Symbology.plainLabel "BRAVO-6")` to the above draws `BRAVO-6`, not the projection (C2).
4. **Projection determinism.** Two tokens differing only in `Health` produce different auto-labels; two with identical channels produce byte-identical auto-labels (C3).
5. **Degenerate projection.** `AutoLabel = Some (Symbology.autoLabel [ShieldFlag])` with `Shield = false` ⇒ no label, no throw (C4).
6. **Rest = static.** A `LabelMotion = Some TypeOn` token at `animate Idle t 0.0` (and `filmstrip` first sample) is byte-identical to the static label (C6).
7. **Motion advances.** The same token at a non-rest phase differs from the rest frame (C7); `Scroll` over an overlong label stays within the region (C8).
8. **Degenerate token.** `{ t with R = 0.0; AutoLabel = …; LabelMotion = Some Pulse }` renders the placeholder without throwing (C11).

Treat this smoke (and the render-bridge tofu test below) — not the plan narrative — as confirmation the capabilities work.

## Run the tests

```bash
dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj
dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj
```

Expected new/extended batteries: `AutoLabelTests.fs` (new), `LabelMotionTests.fs` (new), and deltas in `ChannelPresenceTests`/`DeterminismTests`/`PlaceholderTests`/`GalleryTests`/`LegibilityTests`/`RichLabelTests`, plus the render-bridge tofu case in `RenderLabelTests.fs`. All existing 196–199 goldens stay green.

## Validate against success criteria

| SC | How to validate | Expected |
|---|---|---|
| **SC-001** | Build a roster whose `'stats -> Token` mapping sets `AutoLabel` / `LabelMotion`; render via `render`/`galleryIn`/`filmstripIn` in all three grammars | renders in all grammars; **zero** per-grammar mapping; **zero** entry-point signature change; **zero** per-game stats in the library |
| **SC-002** | Render an auto+motion token through `Symbology.Render.toPng` under the real measurer at sampled phases | every resolved run non-tofu (`Missing = false`); differing projected channel ⇒ distinct labels; distinct frames across phases |
| **SC-003** | Compare opt-out / Plain / no-label / rest-phase tokens to the pinned 199/198/197/pre-feature goldens | byte-identical in every grammar; motion-bound rest frame = static 199 label |
| **SC-004** | Export the same auto/motion `(Token, phase)` twice — same process and a separate process — under a fixed measurer | byte-identical scene data, including each animated frame |
| **SC-005** | Render auto-projected / overlong / typed-on / scrolling / pulsing labels | zero mid-glyph clips, zero overflow into adjacent channels, bounded line count at every phase; explicit overrides auto; empty/degenerate/motion-empty ⇒ zero exceptions |
| **SC-006** | Score a fixed roster with the spec-194 linter, with vs without auto/motion | identical, grammar-independent verdict; pre-attentive governance unchanged |
| **SC-007** | Regenerate surface baselines; run the skill-parity check | only `FS.GG.UI.Symbology.*` baseline moves (else all unchanged + recorded); skill documents auto-label + motion; parity check has zero critical/high findings |

## Regenerate the surface baseline (Tier 1)

The `Symbology.fsi` surface moves (the three new types, two `Token` fields, ctors). Regenerate **only** the symbology baseline and confirm **zero drift** on every other package baseline:

```bash
# regenerate the symbology surface baseline (per the repo's surface-drift tooling)
dotnet test --filter "SurfaceDrift"   # or the repo's baseline-regen entry point
git status readiness/surface-baselines/   # expect ONLY FS.GG.UI.Symbology.* changed
```

## Skill update (FR-022)

Edit `src/Symbology/skill/SKILL.md` (canonical) with the auto-label + label-motion section, then mirror to `.claude/skills/fs-gg-symbology/`, `.agents/skills/fs-gg-symbology/`, and `template/product-skills/fs-gg-symbology/SKILL.md`, and pass:

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx
```
