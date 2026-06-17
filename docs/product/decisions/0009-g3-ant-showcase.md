# 0009 — G3 Ant Design Controls Showcase (new sample vs. extending G1)

**Status**: Accepted · **Feature**: 135 (`specs/135-antd-controls-showcase/`) · **Date**: 2026-06-17

## Context

Workstream G3 demonstrates the shipped `FS.GG.UI.Themes.AntDesign` theme (feature 132) and the
six Ant **enterprise template pages** by rendering the live **96-control** catalog under the Ant
visual language. Like G1 (feature 123) and G2 (feature 134), it is a **package-only consumer** of
`FS.GG.UI.*` (local NuGet feed), kept **outside `FS.GG.Rendering.slnx`** — building it is the
SC-006 public-consumer proof. It is **Tier 2 (additive consumer)**: no public product surface, no
`.fsi`, no design-token, no surface-baseline change (the Ant theme + controls shipped in 132; this
feature only consumes them).

## Decision

Ship G3 as a **new `samples/AntShowcase/` tree**, not as an Ant mode bolted onto the G1
`ControlsGallery`. Rationale (research R8):

1. **G1's golden stays stable.** G1's coverage assertion is 52→10 on Light/Dark; mutating it to
   96 controls + the Ant theme + template pages would rewrite its assertions and golden evidence
   and entangle two features.
2. **G3 needs concepts G1 deliberately excludes** — the `PageKind = Catalog | Template` tag (the
   one justified novelty), the enterprise template pages, and the Ant-only theme set (no
   Default/accent seam).
3. **Two independently demonstrable samples** tell the "one control set, many themes" story better
   (G1 = Default + accent, G3 = Ant light/dark).

Two divergences from the G1 pattern, each justified (plan Complexity Tracking):

1. **`PageKind` + a coverage bijection over Catalog pages only.** Enterprise templates reuse
   controls that already appear on family pages, so they cannot join the "exactly one page"
   bijection. Template pages carry `ControlIds = []` and are instead validated by a
   "composed only of catalog control types" tree-walk (`TemplateTests`); the bijection runs over
   `Kind = Catalog` pages only. Plain F#, no new framework surface.
2. **No accent seam.** Unlike G1, the showcase consumes the shipped `antLight`/`antDark` variants
   verbatim via a thin `AntTheme.resolve : ThemeMode -> Theme`; it never tweaks tokens (FR-016).

The cost — some duplicated shell/evidence scaffolding ported from G1 — is small and was already
accepted for G2.

## Consequences

- A hard precondition: the local feed must be refreshed (repack `FS.GG.Rendering.slnx`) so it
  carries `FS.GG.UI.Themes.AntDesign` and the 96-control `FS.GG.UI.Controls` (research R1 /
  quickstart V0). Documented in the sample README.
- Both product drift gates stay untouched (consumer-only, SC-007).
- The Expecto suite (coverage, page-render, template-composition + form-validation,
  theme-invariance, determinism, degrade, interaction) stays outside the default test tier.
