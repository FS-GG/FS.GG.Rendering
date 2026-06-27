# Phase 0 Research: Refresh fs-gg-ui Template to Current Scene API

This phase resolves *how* the refresh is detected, scoped, and verified. There were no
`NEEDS CLARIFICATION` markers in the spec; the open questions were all "how does the existing
machinery work" and are answered below from the codebase.

## Decision 1 — Drift is detected by generate→build, not by diffing `.fsi`

**Decision**: Treat a real per-profile *generate → restore → build → evidence* run against a freshly
packed feed as the sole authoritative drift signal. Do not derive edits from a raw diff of the
bundled `docs/api-surface/Scene/Scene.fsi` against the live `src/Scene/*.fsi`.

**Rationale**: The bundled `Scene/Scene.fsi` is a single flattened snapshot of the full public Scene
surface (all types + the module), whereas the live source splits the same surface across seven files
(`Types.fsi`, `Scene.fsi`, `Evidence.fsi`, `Inspection.fsi`, `TextShaping.fsi`, `SceneCodec.fsi`,
`Animation.fsi`). A direct file diff therefore reports large "differences" that are pure
organizational layout, not API drift. The compiler is the honest oracle: if the seed code references
a removed/renamed/reshaped construct, the generated product fails to build; if it does not, there is
no drift to fix regardless of what the file diff shows.

**Alternatives considered**:
- *Diff bundled vs live `.fsi` and edit to match* — rejected: noisy (layout differences dominate) and
  it validates the doc, not the code that consumers actually copy.
- *Static grep of `Scene.`/`SceneNode.` usages against the surface* — rejected as primary: useful as a
  cross-check (US2/SC-002) but cannot catch shape/arity changes or Controls/Viewer drift; the build can.

## Decision 2 — Validate by generating each profile; never build `template/base` in place

**Decision**: For every profile in `{app, headless-scene, governed, sample-pack}`, run
`dotnet new fs-gg-ui --profile <p> -o <dir>`, then `dotnet restore` + `dotnet build` + the profile's
evidence/`build.fsx Verify` checks in the generated copy.

**Rationale**: Each seed `.fs` ships *both* a profile branch and its `//#else` counterpart, delimited
by the dotnet template engine's `//#if (profile == …)` / `//#else` / `//#endif` comment directives.
The template engine strips the inactive branch at `dotnet new` time. In `template/base` as committed,
both branches are present, so to the F# compiler the dual definitions collide — `template/base` is not
directly buildable. Drift can only surface in a *generated* product. This also satisfies the spec's
per-profile-independent-validation requirement (US1, edge case "profile-specific code path drifts
independently").

**Alternatives considered**:
- *Build `template/base` directly* — impossible (dual-branch collision), and would not exercise the
  template engine's `copyOnly`/substitution behavior.
- *Validate only the default (`app`) profile* — rejected: profiles select different package sets and
  different seed branches (e.g. headless uses bare `Scene` constructors; app uses the typed Controls
  front door + Viewer hosts), so each must be built (SC-001 = 100% of profiles).

## Decision 3 — Version source and the meaning of "re-pin"

**Decision**: Pack the local feed from the repo's single source version
(`Directory.Build.props` `<Version>`), then set `FsSkiaUiVersion` in
`template/base/Directory.Packages.props` to exactly that produced version. If they already match, the
re-pin is a *verified* no-op recorded as such; the seed-code/API conformance work proceeds regardless.

**Rationale**: The feed packer (`tools/Rendering.Harness -- package-feed`, invoked via
`scripts/refresh-local-feed-and-samples.fsx`) packs `FS.GG.UI.*` to `~/.local/share/nuget-local/` at
the repo `<Version>`. At plan time the repo `<Version>` and the template pin are both
`0.1.0-preview.1` and the local feed is empty, so the pin is not provably stale until a pack runs.
"Re-pin" is therefore an alignment step against whatever the feed actually produces — which is the
durable definition that survives future version bumps (the spec's edge case "current published
version equals the existing pin" is explicitly in scope).

**Alternatives considered**:
- *Hard-code a specific target version (e.g. a `0.1.6x` literal seen in docs)* — rejected: those are
  historical/illustrative literals (e.g. `UPGRADING.md` example), not the live source version; pinning
  to them would break restore against the actual feed.
- *Bump `<Version>` as part of this task* — rejected: version bumps are owned by the merge process
  (`speckit-merge`), not by a template-conformance task (FR-009, no unrelated changes).

## Decision 4 — Scope of "Scene API" includes the surfaces the seed actually consumes

**Decision**: Read "current Scene API" as "the current FS.GG.UI public surface the seed product
consumes," with Scene as the named centerpiece. The app/sample-pack branch additionally consumes
Controls (typed front door), Controls.Elmish, SkiaViewer, Elmish, Layout, KeyboardInput,
DesignSystem, and Themes.Default; these are conformed wherever the per-profile build reports drift.

**Rationale**: The headless/governed branch is pure Scene (`Group`/`Rectangle`/`Text`/`SceneEvidence`/
`LayoutEvidenceReport`). The app branch lowers a typed `Widget` tree via `Widget.toControl`, runs
`ControlsElmish.runInteractiveApp` / `Viewer.runApp`, and emits Viewer evidence. A pin bump moves all
of these in lock-step, so a Scene-only refresh that ignored Controls/Viewer drift would still leave
the `app` and `sample-pack` profiles broken (failing SC-001). The bundled-doc requirement (FR-005)
remains scoped to `docs/api-surface/Scene` as the spec states; other bundled surfaces are conformed
only if the build proves drift.

**Alternatives considered**:
- *Literally Scene-only* — rejected: would leave 2 of 4 profiles non-building, contradicting SC-001.

## Decision 5 — Bundled api-surface docs are hand-maintained / bulk-copied

**Decision**: Keep the bundled `docs/api-surface/**` as hand-maintained `copyOnly` snapshots; refresh
the Scene entry by copying the current public Scene signature, preserving framework identifiers
verbatim (no `sourceName` substitution). Only refresh other bundled surfaces if the per-profile build
exposes a referenced construct that the bundled doc misrepresents.

**Rationale**: There is no automated generator wiring `src/*.fsi` → `template/base/docs/api-surface/`;
`scripts/refresh-surface-baselines.fsx` produces CI drift baselines under `readiness/`, not the
bundled template docs. `template.json` marks `docs/api-surface/**` as `copyOnly` precisely so the
engine does not rewrite framework names (e.g. `ProductDefect`) when scaffolding. The refresh must
respect that (FR-005, US2).

**Alternatives considered**:
- *Wire an automated generator now* — rejected: out of scope (FR-009); a separate tooling feature.

## Verification method summary (feeds Phase 1 quickstart + contracts)

1. Pack feed: `dotnet fsi scripts/refresh-local-feed-and-samples.fsx package-feed` → packages in
   `~/.local/share/nuget-local/`; note the produced version `V`.
2. For each profile `p`: `dotnet new fs-gg-ui --profile p -o <dir>` → `dotnet restore` →
   `dotnet build` → profile evidence (`--scene-evidence`/`--layout-evidence`; app adds
   `--launch-evidence`/`--image-evidence`) and `dotnet fsi build.fsx target Verify`.
3. Re-pin `FsSkiaUiVersion` → `V`; confirm `$(FsSkiaUiVersion)` remains the only FS.GG.UI literal and
   no superseded literal remains (grep).
4. Cross-check seed Scene constructs against the live surface (US2/SC-002) and refresh
   `docs/api-surface/Scene` (FR-005/SC-004).
5. Re-run governance tests + per-profile build/evidence; all green (FR-008/SC-005).

**Output**: all open questions resolved; no `NEEDS CLARIFICATION` remain.
