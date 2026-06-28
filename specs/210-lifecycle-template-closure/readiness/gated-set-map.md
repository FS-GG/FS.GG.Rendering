# Gated-Set + Baseline Reference Map — Feature 210 (T004)

Source of truth: `.template.config/template.json` `sources[]`, read at implementation time
(`FS.GG.UI.Template 0.1.51-preview.1` packs this same `template.json`). Per research.md R3/R4.

## Gated lifecycle set — `condition: lifecycle == "spec-kit"`

Present **only** under `lifecycle == "spec-kit"` (suppressed under `sdd` / `none`):

| source | target | gated root |
|---|---|---|
| `.specify/` | `.specify/` | `.specify/` |
| `template/base/.agents/` | `.agents/` | `.agents/` |
| `template/base/.claude/` | `.claude/` | `.claude/` |
| `.agents/skills/` | `.agents/skills/`, `.claude/skills/` | `.agents/` `.claude/` |
| `.template.config/generated/` | `./` (generated `AGENTS.md` / `CLAUDE.md` + constitution) | agent-context tree |
| `template/product-skills/{fs-gg-scene,fs-gg-skiaviewer,fs-gg-elmish,fs-gg-keyboard-input,fs-gg-ui-widgets,fs-gg-testing}/` | `.agents/skills/<id>/`, `.claude/skills/<id>/` | `.agents/` `.claude/` |
| `template/fragments/samples/skill/` | `.agents/skills/fs-gg-samples/`, `.claude/skills/fs-gg-samples/` | `.agents/` `.claude/` |
| `template/feedback/skill/` | `.agents/skills/fs-gg-feedback-capture/`, `.claude/skills/fs-gg-feedback-capture/` | `.agents/` `.claude/` |
| `template/feedback/extensions/` | `.specify/extensions/feedback/` | `.specify/` |

Collapsed gated roots a filesystem check asserts present/absent:
`.specify/`, `.agents/`, `.claude/`, generated `AGENTS.md`, generated `CLAUDE.md`
(the generated constitution lives under `.specify/`).

## Ungated PRODUCT sources — present for ALL lifecycle values

| source | target | condition |
|---|---|---|
| `template/base/` | `./` | (none — always) |
| `template/fragments/samples/` | `samples/` | `profile == "sample-pack"` |
| `template/design-system/ant/` | `./` | `designSystem == "ant"` |

`profile` and `lifecycle` are orthogonal axes (research R4): the product shape is selected by
`profile`; the gated lifecycle surface is selected by `lifecycle`. Every 3×4 cell is valid.

## Baseline reference for the byte-identical check (research R3)

- **Baseline source**: the pre-lifecycle template output per profile, as captured by Features 204
  (`diff-vs-today=none`) and 206 (PV-3 blocking gate). The default value is `spec-kit`.
- **Scope**: both **file presence** and **file content (bytes)** are compared, across all four
  profiles (`app`, `headless-scene`, `governed`, `sample-pack`).
- **Operational restatement here (210)**: because the pre-lifecycle baseline == today's `spec-kit`
  default (204 proved `diff-vs-today=none`), the published-package check asserts the **no-flag
  default == explicit `--lifecycle spec-kit`** byte-for-byte for every profile; that equality is the
  reproducible stand-in for "identical to the pre-lifecycle baseline" against the installed package.
