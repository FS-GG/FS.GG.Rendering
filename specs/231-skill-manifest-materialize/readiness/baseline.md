# Pre-change live baseline — Feature 231 (T002)

- **Date**: 2026-07-02 · **Provenance: live** (`dotnet new install .` on the unmodified
  `main`-equivalent tree at template `0.1.60-preview.1`; `dotnet new fs-gg-ui --name Zebra
  --profile app` — default `lifecycle=spec-kit`, default `designSystem=wcag`).
- **Gitignore allowlist proof**: `git check-ignore specs/231-skill-manifest-materialize/readiness/baseline.md`
  → exit 1 (not ignored) after adding the `!specs/231-.../readiness/**` allowlist lines.

## F3 confirmed live (dangling dev-surface vendoring)

Each of the three roots (`.agents/skills/`, `.claude/skills/`, `.codex/skills/`) carries the
same **41** directories: 16 `speckit-*` + 25 `fs-gg-*`. Of the 25, only 8 are canonical
product-skill bodies (scene, symbology, skiaviewer, elmish, keyboard-input, ui-widgets,
styling, layout — overwritten from `template/product-skills/`). The other **17 are ~12-line
wrappers routing to repo-internal paths absent in the product**:

- 9 aliases: `fs-gg-product-{elmish,keyboard-input,layout,scene,skiaviewer,styling,symbology,testing,ui-widgets}` → `../../../template/product-skills/…`
- 8 framework/dev + profile-mismatched wrappers: `fs-gg-ant-design` (→ `../../../.claude/skills/…`, self-referential in product), `fs-gg-design-system`, `fs-gg-diagnostics`, `fs-gg-generated-controls-guidance` (→ `../../../src/**` / `../../../template/fragments/**`), `fs-gg-project` (→ `../../../template/base/…`, shadowing the real base body), and — in the **app** profile, where their canonical bodies do not ship — `fs-gg-samples`, `fs-gg-testing`, `fs-gg-feedback-capture` (→ `../../../template/**`).

Verified sample (`.agents/skills/fs-gg-diagnostics/SKILL.md`): "read the canonical
instructions in: `../../../src/Diagnostics/skill/SKILL.md`" — no `src/Diagnostics` exists in
the scaffold.

## F5 confirmed live (lowercase-name rewriting of skill prose)

`.agents/skills/fs-gg-scene/SKILL.md` in the Zebra scaffold:
- line 3: "Build pure scene descriptions in a generated FS.GG.UI **zebra**."
- line 10: "Use this skill for **zebra** code that builds pure `Scene` / `SceneNode`"

## Feature 230 mechanism confirmed

Three-root parity at generation is produced by the blanket + 24 per-skill twin rows (41 = 41
= 41 above) — the hand-maintained matrix this feature replaces.
