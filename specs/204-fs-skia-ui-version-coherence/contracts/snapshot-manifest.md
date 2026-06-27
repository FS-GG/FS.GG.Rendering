# Contract: Reproducible Snapshot (US2 — FR-003)

The pinned `FsSkiaUiVersion` MUST refer to an **immutable, reproducible** snapshot, not a moving feed.
Mechanism (maintainer-selected): **git tag + committed lockfile + recorded manifest**.

## SM-1 — Git tag

An **annotated** tag `fs-skia-ui/v<version>` (e.g. `fs-skia-ui/v0.1.51-preview.1`) at the resolution
commit — the commit whose pack produced the pinned set. Checking out the tag and re-packing reproduces
the same source.

```sh
git tag -a fs-skia-ui/v<version> -m "coherent FS.GG.UI.* snapshot for fs-gg-ui template pin"
git push origin fs-skia-ui/v<version>
```

## SM-2 — Recorded manifest

This file's table records the snapshot: the **16 real** `FS.GG.UI.*` package IDs, each @ `0.1.50-preview.1`.
`FS.GG.UI.Color` and `FS.GG.UI.SkillSupport` are **absent by design** (retired / no producer — see
research R2). To be filled with the verified version at task time:

| # | Package ID | Version |
|---|------------|---------|
| 1 | FS.GG.UI.Build | `0.1.50-preview.1` |
| 2 | FS.GG.UI.Scene | `0.1.50-preview.1` |
| 3 | FS.GG.UI.Canvas | `0.1.50-preview.1` |
| 4 | FS.GG.UI.Controls | `0.1.50-preview.1` |
| 5 | FS.GG.UI.Controls.Elmish | `0.1.50-preview.1` |
| 6 | FS.GG.UI.DesignSystem | `0.1.50-preview.1` |
| 7 | FS.GG.UI.Diagnostics | `0.1.50-preview.1` |
| 8 | FS.GG.UI.Elmish | `0.1.50-preview.1` |
| 9 | FS.GG.UI.KeyboardInput | `0.1.50-preview.1` |
| 10 | FS.GG.UI.Layout | `0.1.50-preview.1` |
| 11 | FS.GG.UI.SkiaViewer | `0.1.50-preview.1` |
| 12 | FS.GG.UI.Symbology | `0.1.50-preview.1` |
| 13 | FS.GG.UI.Symbology.Render | `0.1.50-preview.1` |
| 14 | FS.GG.UI.Testing | `0.1.50-preview.1` |
| 15 | FS.GG.UI.Themes.AntDesign | `0.1.50-preview.1` |
| 16 | FS.GG.UI.Themes.Default | `0.1.50-preview.1` |

> The template's *seed* references a subset (Scene always; SkiaViewer/Elmish/KeyboardInput/Layout/
> Controls/Controls.Elmish/DesignSystem/Themes.Default for app/sample-pack; Testing for governed). The
> snapshot records the full coherent set so the registry can reference one immutable version.

## SM-3 — Committed lockfile

`packages.lock.json` committed for the template (per generated profile, or a template-level lock), with
locked restore enabled (`RestorePackagesWithLockFile=true` + `RestoreLockedMode=true` on CI/verify), so
the *resolved graph* is byte-reproducible — not only the source.

## Pass conditions

| ID | Condition | Maps to |
|----|-----------|---------|
| SM-A | All 16 real IDs exist in the feed at `0.1.50-preview.1` (one coherent set; no phantom IDs). | FR-003, US2 AS1 |
| SM-B | Restoring the pinned template from a clean cache **twice** yields the identical resolved set. | SC-002, US2 AS2 |
| SM-C | `pinned-version == tag version == every-manifest-row version`. | FR-003, data-model invariant |
| SM-D | The pin does **not** depend on un-snapshotted HEAD: re-checkout of the tag reproduces the set. | FR-003, edge case "HEAD advances" |

## Re-drift guard

If framework HEAD later advances past `0.1.50-preview.1`, the pin keeps referencing this tagged snapshot.
Re-drift is a **new** cross-repo request, not a reason to re-open #1 (Out of Scope).
