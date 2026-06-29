# T012 — Org feed serves `V` (FR-005, SC-001, INV-3) — GREEN

**Captured**: 2026-06-29, live against `nuget.pkg.github.com/FS-GG` via `gh api`.

```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'
0.1.53-preview.1
0.1.52-preview.1
```
✅ `0.1.53-preview.1` is **served** and is strictly `> 0.1.52-preview.1` (INV-1).

## Coherent set at the same `V` (FR-006, INV-2) — sampled

```
FS.GG.UI.Template -> 0.1.53-preview.1, 0.1.52-preview.1
FS.GG.UI.Scene    -> 0.1.53-preview.1, 0.1.52-preview.1
FS.GG.UI.Controls -> 0.1.53-preview.1, 0.1.52-preview.1
FS.GG.UI.Elmish   -> 0.1.53-preview.1, 0.1.52-preview.1
FS.GG.UI.Testing  -> 0.1.53-preview.1, 0.1.52-preview.1
```
✅ Every sampled `FS.GG.UI.*` package **and** the template serve the same `V` — the coherent-set
invariant holds.
