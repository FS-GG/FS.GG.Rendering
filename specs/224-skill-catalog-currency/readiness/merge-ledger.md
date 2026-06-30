# Merge ledger (Feature 224)

Squash-merged `224-skill-catalog-currency` → `main` as `ae3ae13` (from feature commit `761dce2`);
pushed `54d61eb..ae3ae13`; local feature branch deleted (no remote branch existed). Captured 2026-06-30.

## Package bump / local feed — NONE (deliberate, operator-confirmed)

This is a **content-only** change to the `fs-gg-ui-template` package surface
(`template/base/docs/skillist-reference.md` + `scaffold-map.md` + a `Package.Tests` check). The
FS.GG.UI.* **library** projects are untouched.

- **No version bump**: pins stay at `0.1.55-preview.1` (`.template.package/FS.GG.UI.Template.fsproj`
  `<Version>`, `template/base/Directory.Packages.props` `<FsGgUiVersion>`). The repo convention moves
  the template version and the FS.GG.UI.* set **together** as one coherent republish (Features 222/223);
  a partial local bump would point the generated product's `FsGgUiVersion` at lib packages not packed
  to the local feed — an incoherent feed. Avoided.
- **No local feed pack / sample pin alignment**: not performed, because no new package version exists.
  Sample pins remain coherent against the unchanged `0.1.55-preview.1` set.
- **Delivery deferred to the next republish** (matches the feature plan / research R4): the coherent-set
  bump + full pack + org-feed release + `fs-gg-ui-template` registry version/coherence flip ride the
  next `fs-gg-ui-template` republish (successor to the closed #33; same pattern as #35 / Feature 223).
  Tracked on coordination issue #36 (commented) and the Coordination board (#36 → `In review`).

## Caveat

Until that republish, the corrected catalog lives on `main` but is **not yet on the org feed**; the
registry still records `0.1.55-preview.1`. This is the normal merged-but-unreleased state, visibly
tracked — not reported as delivered.

## Test/baseline evidence

See `quickstart-evidence.md` (6/6 currency check green; Feature 219/204 gating green; baseline
before/after identical, no new reds; `Package.Tests` 153→159 passed with the same 1 known
`Build engine baseline` red).
