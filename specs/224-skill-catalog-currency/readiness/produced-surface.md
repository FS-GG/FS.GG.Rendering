# Produced-surface ground truth (T005 live run + T006 confirmation)

**Standing assumption discharged.** Catalog rows are authored from what a *scaffolded product*
actually carries, NOT from the framework-repo `src/**/skill` layout. Captured 2026-06-30.

## T005 — live scaffold run

The installed template is the published package `fs.gg.ui.template@0.1.55-preview.1` (the current
coherent preview; this feature has not yet shipped). Two real scaffolds via `dotnet new fs-gg-ui`:

```
dotnet new fs-gg-ui --name Catalog1 --profile app --lifecycle spec-kit -o <scratch>/sk-app
dotnet new fs-gg-ui --name Catalog2 --profile app --lifecycle sdd      -o <scratch>/sdd-app
```

### spec-kit / app — raw `.agents/skills/` `name:` listing (38 dirs; `.claude/skills/` identical, 38)

```
catalog1-widgets  fs-gg-ant-design  fs-gg-design-system  fs-gg-diagnostics  fs-gg-elmish
fs-gg-feedback-capture  fs-gg-generated-controls-guidance  fs-gg-keyboard-input  fs-gg-layout
fs-gg-product-elmish  fs-gg-product-keyboard-input  fs-gg-product-scene  fs-gg-product-skiaviewer
fs-gg-product-symbology  fs-gg-product-testing  fs-gg-product-ui-widgets  fs-gg-project
fs-gg-samples  fs-gg-scene  fs-gg-skiaviewer  fs-gg-symbology  fs-gg-testing
speckit-agent-context-update  speckit-analyze  speckit-checklist  speckit-clarify
speckit-constitution  speckit-git-commit  speckit-git-feature  speckit-git-initialize
speckit-git-remote  speckit-git-validate  speckit-implement  speckit-merge  speckit-plan
speckit-specify  speckit-tasks  speckit-taskstoissues
```

- `docs/skillist-reference.md` → **PRESENT** under spec-kit.
- Under spec-kit the entire repo-root `.agents/skills/` tree is copied verbatim (template.json
  lines 220–235), so the produced surface is the full repo skill set + per-profile product overlay.

### sdd / app

- `.agents/skills/` → only 6 ungated base skills; `docs/skillist-reference.md` → **ABSENT**.

This confirms emission gating (Feature 219) is intact: catalog present under spec-kit, absent under
sdd/none. None of the 8 defunct `fs-gg-*`, `fsdocs-*`, `fsharp-*` ids appear in any produced surface.

## ⚠️ Separate finding (NOT this feature's scope) — `fs-gg-ui` → product-name substitution

The product skill `fs-gg-ui-widgets` ships in a directory `.agents/skills/fs-gg-ui-widgets/` whose
`SKILL.md` `name:` is rewritten to **`catalog1-widgets`** (the `"replaces": "fs-gg-ui"` symbol in
`.template.config/template.json:170` substitutes the product name into the skill's own `name:`
frontmatter). The directory and path are intact; only the `name:` is mangled. This is a pre-existing
template-substitution defect, distinct from the catalog-currency defect this feature fixes (it
mangles a *shipping* skill's id rather than listing a *non-existent* id). The currency check is
scoped (per plan/contract) to repo-side `SkillParity` discovery, where `fs-gg-ui-widgets` resolves
correctly. **Flagged for cross-repo coordination follow-up (see #36 comment / sibling epic #34); not
fixed here** to keep this change minimal and within its contracted scope.

## T006 — confirmed shipping set (R1) + decisions

**Confirmed resolvable shipping set the catalog lists:**

- Product skills (profile-wired, consumer paths `.agents/skills/<id>/`):
  `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`,
  `fs-gg-testing`, `fs-gg-ui-widgets`.
- `speckit-*` command skills (co-ship under spec-kit; 16 discoverable):
  `speckit-agent-context-update`, `speckit-analyze`, `speckit-checklist`, `speckit-clarify`,
  `speckit-constitution`, `speckit-git-commit`, `speckit-git-feature`, `speckit-git-initialize`,
  `speckit-git-remote`, `speckit-git-validate`, `speckit-implement`, `speckit-merge`,
  `speckit-plan`, `speckit-specify`, `speckit-tasks`, `speckit-taskstoissues`.

**`speckit-*` discoverability (closes analysis finding U1):** confirmed — each `speckit-*` id above
is surfaced by `SkillParity.discoverDefaultSurfaces`/`inventorySkills` via the `spec-kit-command`
surface (a `SKILL.md` whose `name:` equals the id), not merely as a directory. The check (T009) can
therefore resolve valid `speckit-*` refs.

**R2 decision: Option A — hand-maintained-under-check, with an honest header.** The row set is small
and mechanical; a generator (Option B) is not warranted (Principle III). The currency check
(`Feature224SkillCatalogCurrencyTests.fs`) enforces it; the refresh path is "edit to satisfy the
check".
