# Skillist reference

<!-- HAND-MAINTAINED, enforced by the Feature 224 skill-catalog currency check
     (tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs). This file is NOT generated:
     there is no SkillRegistry, RefreshSurfaceBaselines target, or TargetMetadataDrift check — that
     earlier provenance was fiction (research R0). Every `id` listed below must resolve to a real
     SKILL.md (a SKILL.md whose `name:` equals the id) that this product actually carries, and every
     path must be a consumer location (.agents/skills/<id>/ or .claude/skills/<id>/). To refresh:
     edit the rows to match the skills the product ships, then run
     `dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency` until green. The
     check fails — naming the id, doc, and line — if any row dangles. -->

A task's declared `skillist` ids (and the `[skillist: …]` mirror in `tasks.md`) are the `name:`
value from the owning `SKILL.md`, not the directory name. This page lists the skills a generated
spec-kit product carries, with the path where each resolves **in your product**.

This catalog ships only under the **spec-kit** lifecycle (under `sdd`/`none` it is suppressed, since
the spec-kit authoring skills it co-lists are absent there).

## Product capability skills

These vendor the FS.GG.UI capability guidance into your product. They are wired **per profile**, so
not every one is present in every product — the "Profiles" column lists the profiles that vendor
each. Each is a self-contained canonical body declared (with its content digest) in
`.agents/skills/skill-manifest.json`; no wrapper aliases ship (Feature 231 / ADR-0014).

| skillist id (`name:`) | resolved SKILL.md path | Profiles |
|---|---|---|
| `fs-gg-scene` | .agents/skills/fs-gg-scene/SKILL.md | app, headless-scene, governed, sample-pack, game |
| `fs-gg-symbology` | .agents/skills/fs-gg-symbology/SKILL.md | app, headless-scene, governed, sample-pack, game |
| `fs-gg-skiaviewer` | .agents/skills/fs-gg-skiaviewer/SKILL.md | app, sample-pack, game |
| `fs-gg-elmish` | .agents/skills/fs-gg-elmish/SKILL.md | app, sample-pack, game |
| `fs-gg-keyboard-input` | .agents/skills/fs-gg-keyboard-input/SKILL.md | app, game |
| `fs-gg-ui-widgets` | .agents/skills/fs-gg-ui-widgets/SKILL.md | app, game |
| `fs-gg-styling` | .agents/skills/fs-gg-styling/SKILL.md | app, game |
| `fs-gg-layout` | .agents/skills/fs-gg-layout/SKILL.md | app, game |
| `fs-gg-testing` | .agents/skills/fs-gg-testing/SKILL.md | governed |

> Under the spec-kit lifecycle the product additionally carries the base authoring skill
> `fs-gg-project` (and, when selected, `fs-gg-samples` for the sample-pack profile and
> `fs-gg-feedback-capture` with `--feedback true`) at the same `.agents/skills/<id>/` locations.
> The framework's own developer surface (`fs-gg-product-*` aliases, `fs-gg-diagnostics`,
> `fs-gg-design-system`, …) stays in the FS.GG.Rendering repository and does not ship
> (Feature 231 / ADR-0014). After the first build the same union is materialized byte-identically
> into `.claude/skills/` and `.codex/skills/` — see `docs/scaffold-map.md`.

## Spec Kit command skills

The `speckit-*` workflow command skills co-ship under the spec-kit lifecycle. Use these ids when a
task `owns:` an authoring step (e.g. task generation → `speckit-tasks`, implementation loading →
`speckit-implement`).

| skillist id (`name:`) | resolved SKILL.md path |
|---|---|
| `speckit-specify` | .agents/skills/speckit-specify/SKILL.md |
| `speckit-clarify` | .agents/skills/speckit-clarify/SKILL.md |
| `speckit-plan` | .agents/skills/speckit-plan/SKILL.md |
| `speckit-tasks` | .agents/skills/speckit-tasks/SKILL.md |
| `speckit-taskstoissues` | .agents/skills/speckit-taskstoissues/SKILL.md |
| `speckit-analyze` | .agents/skills/speckit-analyze/SKILL.md |
| `speckit-checklist` | .agents/skills/speckit-checklist/SKILL.md |
| `speckit-constitution` | .agents/skills/speckit-constitution/SKILL.md |
| `speckit-implement` | .agents/skills/speckit-implement/SKILL.md |
| `speckit-merge` | .agents/skills/speckit-merge/SKILL.md |
| `speckit-agent-context-update` | .agents/skills/speckit-agent-context-update/SKILL.md |
| `speckit-git-commit` | .agents/skills/speckit-git-commit/SKILL.md |
| `speckit-git-feature` | .agents/skills/speckit-git-feature/SKILL.md |
| `speckit-git-initialize` | .agents/skills/speckit-git-initialize/SKILL.md |
| `speckit-git-remote` | .agents/skills/speckit-git-remote/SKILL.md |
| `speckit-git-validate` | .agents/skills/speckit-git-validate/SKILL.md |
