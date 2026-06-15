# Skillist reference

<!-- GENERATED from the live SkillRegistry + Audit.ownsVocabulary (feature 062, FR-006).
     The valid `skillist` ids are the SKILL.md `name:` values (NOT the directory name),
     resolved here so authors never grep '^name:' each file. Do not edit by hand;
     regenerate with ./fake.sh build -t RefreshSurfaceBaselines. Currency-checked by
     TargetMetadataDrift. -->

A task's declared `skillist` ids (and the `[skillist: …]` mirror in `tasks.md`) are the
`name:` value from the owning `SKILL.md`, not the directory name. This page lists the valid
ids resolved from the live registry and the closed `owns:`→implied-skill table.

## Valid `skillist` ids

| skillist id (`name:`) | resolved SKILL.md path |
|---|---|
| `fs-gg-controls-host` | .agents/skills/fs-gg-controls-host/SKILL.md |
| `fs-gg-design-tokens` | .agents/skills/fs-gg-design-tokens/SKILL.md |
| `fs-gg-elmish` | src/Elmish/skill/SKILL.md |
| `fs-gg-evidence-mode` | .agents/skills/fs-gg-evidence-mode/SKILL.md |
| `fs-gg-generated-controls-guidance` | template/fragments/controls/skill/SKILL.md |
| `fs-gg-keyboard-input` | src/KeyboardInput/skill/SKILL.md |
| `fs-gg-layout` | src/Layout/skill/SKILL.md |
| `fs-gg-layout-readability` | .agents/skills/fs-gg-layout-readability/SKILL.md |
| `fs-gg-reconciliation` | .agents/skills/fs-gg-reconciliation/SKILL.md |
| `fs-gg-samples` | template/fragments/samples/skill/SKILL.md |
| `fs-gg-scene` | src/Scene/skill/SKILL.md |
| `fs-gg-skiaviewer` | src/SkiaViewer/skill/SKILL.md |
| `fs-gg-template-update` | .agents/skills/fs-gg-template-update/SKILL.md |
| `fs-gg-testing` | src/Testing/skill/SKILL.md |
| `fs-gg-typed-controls` | .agents/skills/fs-gg-typed-controls/SKILL.md |
| `fs-gg-ui-widgets` | src/Controls/skill/SKILL.md |
| `fs-gg-viewer-host` | .agents/skills/fs-gg-viewer-host/SKILL.md |
| `fsdocs-api-doc` | .agents/skills/fsdocs-api-doc/SKILL.md |
| `fsdocs-build` | .agents/skills/fsdocs-build/SKILL.md |
| `fsdocs-examples` | .agents/skills/fsdocs-examples/SKILL.md |
| `fsdocs-setup` | .agents/skills/fsdocs-setup/SKILL.md |
| `fsdocs-technical` | .agents/skills/fsdocs-technical/SKILL.md |
| `fsharp-build-orchestration` | .agents/skills/fsharp-build-orchestration/SKILL.md |
| `fsharp-code-generation` | .agents/skills/fsharp-code-generation/SKILL.md |
| `fsharp-graph-algorithms` | .agents/skills/fsharp-graph-algorithms/SKILL.md |
| `fsharp-io-globbing` | .agents/skills/fsharp-io-globbing/SKILL.md |
| `fsharp-parsing` | .agents/skills/fsharp-parsing/SKILL.md |
| `fsharp-shell-process` | .agents/skills/fsharp-shell-process/SKILL.md |
| `speckit-analyze` | .agents/skills/speckit-analyze/SKILL.md |
| `speckit-archive-readiness` | .agents/skills/speckit-archive-readiness/SKILL.md |
| `speckit-checklist` | .agents/skills/speckit-checklist/SKILL.md |
| `speckit-clarify` | .agents/skills/speckit-clarify/SKILL.md |
| `speckit-constitution` | .agents/skills/speckit-constitution/SKILL.md |
| `speckit-evidence-audit` | .agents/skills/speckit-evidence-audit/SKILL.md |
| `speckit-evidence-graph` | .agents/skills/speckit-evidence-graph/SKILL.md |
| `speckit-git-commit` | .agents/skills/speckit-git-commit/SKILL.md |
| `speckit-git-feature` | .agents/skills/speckit-git-feature/SKILL.md |
| `speckit-git-initialize` | .agents/skills/speckit-git-initialize/SKILL.md |
| `speckit-git-remote` | .agents/skills/speckit-git-remote/SKILL.md |
| `speckit-git-validate` | .agents/skills/speckit-git-validate/SKILL.md |
| `speckit-implement` | .agents/skills/speckit-implement/SKILL.md |
| `speckit-merge` | .agents/skills/speckit-merge/SKILL.md |
| `speckit-plan` | .agents/skills/speckit-plan/SKILL.md |
| `speckit-specify` | .agents/skills/speckit-specify/SKILL.md |
| `speckit-tasks` | .agents/skills/speckit-tasks/SKILL.md |
| `speckit-taskstoissues` | .agents/skills/speckit-taskstoissues/SKILL.md |

## Directory-name → accepted `skillist` id

| directory-like name | accepted id (`name:`) | SKILL.md |
|---|---|---|
| `Controls` | `fs-gg-ui-widgets` | src/Controls/skill/SKILL.md |
| `Elmish` | `fs-gg-elmish` | src/Elmish/skill/SKILL.md |
| `KeyboardInput` | `fs-gg-keyboard-input` | src/KeyboardInput/skill/SKILL.md |
| `Layout` | `fs-gg-layout` | src/Layout/skill/SKILL.md |
| `Scene` | `fs-gg-scene` | src/Scene/skill/SKILL.md |
| `SkiaViewer` | `fs-gg-skiaviewer` | src/SkiaViewer/skill/SKILL.md |
| `Testing` | `fs-gg-testing` | src/Testing/skill/SKILL.md |
| `controls` | `fs-gg-generated-controls-guidance` | template/fragments/controls/skill/SKILL.md |
| `samples` | `fs-gg-samples` | template/fragments/samples/skill/SKILL.md |

## Closed `owns:` vocabulary → implied skill

An `owns:` value (in `tasks.deps.yml`) requires its implied skill in the task's `skillist`.
The vocabulary is a closed set; an unknown value is a directive error.

| `owns:` value | implied skill |
|---|---|
| `graph-validation` | `speckit-evidence-graph` |
| `evidence-audit` | `speckit-evidence-audit` |
| `task-generation` | `speckit-tasks` |
| `implementation-loading` | `speckit-implement` |
| `constitution` | `speckit-constitution` |

