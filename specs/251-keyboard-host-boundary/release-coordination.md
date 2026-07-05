# Release coordination — feature 251 (STAGED; flip at merge)

**Status**: STAGED — do **not** flip until this feature is released. This is a **template-content**
change (the emitted `game`/`sample-pack` product gains a keyboard-only host-boundary comment at its
input-wiring site, and the shipped `fs-gg-keyboard-input` product-skill + fragment gain a
"Capability boundary" note), so it follows the **publish-before-flip** protocol
(`cross-repo-coordination` skill), exactly like sibling #138.

## What ships

- `FS.GG.UI.Template` republish carrying: the game-branch `Model.fs` input-wiring comment, the
  `fs-gg-keyboard-input/SKILL.md` + `keyboard-input/README.md` capability-boundary note, the
  `scaffold-map.md` pre-design pointer, the regenerated `skill-manifest.json` digest (existing skill
  id `fs-gg-keyboard-input`; body changed), and the new generated-product assertion in
  `BehaviorTests.fs`.
- **No** `FS.GG.UI.*` **library** package changes (libraries stay Tier 2 — `SkiaViewer`/`KeyboardInput`
  are *described*, not modified). **No** new skill id. **No** durable/governance-scanned host wiring
  change (`Program.fs` and the durable spine untouched).

## Publish-before-flip steps (at release, via `cross-repo-coordination`)

1. Release `FS.GG.UI.Template` at the new preview version (`speckit-merge` bumps + packs the local feed).
2. In `FS-GG/.github`, draft the coherent-set updates **after** the package is live:
   - `registry/dependencies.yml` — bump the `fs-gg-ui-template` contract/package version.
   - `registry/CHANGELOG.md` — one dated, newest-first entry (keyboard-only host boundary surfaced in
     the game starter + keyboard-input skill).
   - `docs/registry/compatibility.md` — refresh the relevant rows.
3. Close `FS-GG/FS.GG.Rendering#139`, check its box on epic `#137`, and let the board roll the epic up
   to Done (last open child); move the board items to Done via
   `scripts/fsgg-coord done <issue> --flip`.

## Board

Item #139 (`FS-GG/FS.GG.Rendering`) is **In progress** on the org Coordination board. Child of epic
#137 and its **last open child** (#138 — collision-safe Vec2 — already Done). Closing #139 completes
epic #137, which rolls up to Done automatically on the final child's `--flip`.
