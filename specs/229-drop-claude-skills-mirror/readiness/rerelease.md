# Re-release + cross-repo follow-ons (T018 / T023) — Feature 229

## Version bump (FR-008 / SC-006)

- `.template.package/FS.GG.UI.Template.fsproj` `<Version>`: `0.1.58-preview.1` → `0.1.59-preview.1`.
- The coherent set's next preview ships Feature 228 (unreleased) + Feature 229 together.
- Framework pin `template/base/Directory.Packages.props` `<FsGgUiVersion>` **unchanged** at `0.1.58-preview.1`:
  this feature makes no `src/` change, so scaffolded products consume the already-published framework 0.1.58.
  Feature 209 version-coherence gate: **green** (7/7).
- Local pack proved installable during validation (`0.1.58-dev229post.2`); the org-feed pack + publish is
  performed by the merge/release lane (`/speckit-merge`) and CI-on-tag (`fs-gg-ui-template/v0.1.59-preview.1`).

## Cross-repo follow-ons (publish-before-flip — NOT this repo's code)

Closes the remaining half of #42's DoD once the package is published:

1. **Registry flip** — `FS-GG/.github` registry `fs-gg-ui-template` entry → `0.1.59-preview.1`
   (+ compatibility note). Coherence id `agent-skill-mirror` (ADR-0011).
2. **Templates re-pin** — `FS.GG.Templates` `providers/rendering.providers.yml` → `0.1.59-preview.1`;
   the `new-sdd-fullstack` composition gate goes green (closes Templates#47's Rendering half).
3. **Orchestrator half (parallel)** — `FS.GG.SDD#57` (`fsgg-sdd 0.4.0`) ships the three-root union fan-out;
   both halves must be published before a full-stack scaffold is clean end-to-end (ADR-0011 ordering).

These are tracked on the org Coordination board via the `cross-repo-coordination` protocol; file/advance
them after this branch merges and the package is on the org feed.
