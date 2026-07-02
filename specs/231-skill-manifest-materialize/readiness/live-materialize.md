# Live materialize proof — Feature 231 (T009)

- **Date**: 2026-07-02 · **Provenance: live** (`dotnet new install . --force` of the reworked
  template; `dotnet new fs-gg-ui --name Zebra --profile app` — spec-kit default; real
  `dotnet build Zebra.slnx` with real NuGet restore of the pinned FS.GG.UI 0.1.58-preview.1 set).
- Caveat: after the first build exposed the MSB3371 stamp defect, the fixed
  `Directory.Build.props` (added `<MakeDir>` before `<Touch>`) was copied into the live
  scaffold rather than re-scaffolding; all other bytes are the template's emission.

## At generation (before any build)

- `.agents/skills/` = 16 `speckit-*` + 9 product skills (app profile: elmish, keyboard-input,
  layout, project, scene, skiaviewer, styling, symbology, ui-widgets) + `skill-manifest.json`.
  **Zero wrapper dirs** (baseline.md had 41 dirs incl. 17 wrappers).
- `.claude/skills/` = `fs-gg-project` only (base tree); `.codex/` absent; the vendored
  materialize pair emitted at `.specify/scripts/fs-gg/`.
- Verbatim emission proven: `diff` of scaffold `fs-gg-scene`/`fs-gg-project` `SKILL.md` vs
  canonical sources = identical; `grep -c zebra fs-gg-scene/SKILL.md` = 0 while
  "generated FS.GG.UI product" survives (F5 fixed for skills).

## Materialize + verify (driver, enforcing)

- `dotnet fsi .specify/scripts/fs-gg/materialize-skill-roots.fsx --enforce` →
  `fs-gg-skill-roots: ok (25 skills, 53 files mirrored)`, exit 0.
- Idempotence: immediate rerun → `ok (25 skills, 0 files mirrored)`.
- `diff -r` `.agents/skills` ≡ `.claude/skills` ≡ `.codex/skills` → identical (incl.
  `fs-gg-symbology/reference.fsx` and `skill-manifest.json`).
- Red case (digest mismatch): planting a manifest with `sha256: "deadbeef"` for `fs-gg-scene`
  → `DRIFT fs-gg-scene … hash-mismatch=[.claude; .codex; .agents]`, exit 1 under `--enforce`
  (exercised on the T002 baseline scaffold).

## MSBuild target (`FsGgMaterializeSkillRoots`, first `dotnet build`)

- With `.codex/` and the stamp deleted: `dotnet build Zebra.slnx` →
  `fs-gg-skill-roots: ok (25 skills, 27 files mirrored)` + **Build succeeded**;
  `.codex/skills` repopulated (25 dirs + manifest).
- Incrementality: second `dotnet build` runs the materialize **0** times (stamp up to date).
- Post-build: three roots byte-identical; `fs-gg-scene` `.codex` copy digest
  `37ef7800…0da7` == manifest `sha256` (spot check).
