# Coherence-surface map (T004) + early live-smoke evidence (T005/T006)

Regenerated manifest of the live inputs the version-coherence verdict checks, recorded as the
expected values against the **current** tree. Authoritative current state captured 2026-06-28 on
branch `209-version-staleness-guard`.

## Single source of version truth (`SingleVersionSource`)

| Field | Value |
|-------|-------|
| literal location | `template/base/Directory.Packages.props:9` |
| `<FsGgUiVersion>` | `0.1.50-preview.1` |
| occurrences | **1** (D3 single-literal hypothesis CONFIRMED — `grep -c` == 1) |

## Coherent snapshot tags (`CoherentSnapshotTag`)

`git tag --list 'fs-gg-ui/v*'`:

- `fs-gg-ui/v0.1.50-preview.1`
- `fs-gg-ui/v0.1.51-preview.1`  ← **latest** (preview-aware order)

> Note: the pin (`0.1.50-preview.1`) currently lags the latest tag (`0.1.51-preview.1`). This is the
> live state of the tree; whether it is a `pin-lags-tag` drift depends on policy — see the verdict's
> handling of the latest-tag rule. The forced-drift fixtures use values strictly below `0.1.50`.

## Published member set `P` (16 packable `FS.GG.UI.*` under `src/**`)

Discovered (IsPackable=true, PackageId prefix `FS.GG.UI.`):

```
FS.GG.UI.Build            FS.GG.UI.Diagnostics      FS.GG.UI.Symbology
FS.GG.UI.Canvas           FS.GG.UI.Elmish           FS.GG.UI.Symbology.Render
FS.GG.UI.Controls         FS.GG.UI.KeyboardInput    FS.GG.UI.Testing
FS.GG.UI.Controls.Elmish  FS.GG.UI.Layout           FS.GG.UI.Themes.AntDesign
FS.GG.UI.DesignSystem     FS.GG.UI.Scene            FS.GG.UI.Themes.Default
                          FS.GG.UI.SkiaViewer
```

Cardinality: **16**.

## BOM dependency set `B` (`src/Meta/FS.GG.UI.nuspec`)

16 `<dependency>` ids, every `version="[$version$]"` (single token, exact bracket, no comma).
`B.ids == P.members` (16/16 parity CONFIRMED).

## Template consumed pins `T` (`template/base/Directory.Packages.props`)

11 `FS.GG.UI.*` `PackageVersion` entries, every `Version="$(FsGgUiVersion)"` (no hardcoded literal):

```
FS.GG.UI.Build  FS.GG.UI.Scene  FS.GG.UI.SkiaViewer  FS.GG.UI.Elmish  FS.GG.UI.KeyboardInput
FS.GG.UI.Layout  FS.GG.UI.Controls  FS.GG.UI.Controls.Elmish  FS.GG.UI.DesignSystem
FS.GG.UI.Themes.Default  FS.GG.UI.Testing
```

Cardinality: **11**. `T.pins ⊆ P.members` (CONFIRMED). The 16-vs-11 gap is intentional (D6):
`Canvas`, `Diagnostics`, `Symbology`, `Symbology.Render`, `Themes.AntDesign` are published but not
consumed by the default generated product.

## Runtime resolution (`build.fsx:60`)

`template/base/build.fsx:60` regex `<FsGgUiVersion>([^<]+)</FsGgUiVersion>` matches the literal in
`Directory.Packages.props` (CONFIRMED present at the documented line).

---

## T006 — early smoke: the 204 drift is currently SILENT (premise CONFIRMED)

Two independent confirmations that a stale `<FsGgUiVersion>` produces **no red** in today's tree:

1. **Structural:** `FS.GG.Rendering.slnx` does **not** include `template/base/**` (it is the
   generated-product template, not a solution member). The repo's own `dotnet build` /
   `dotnet test` path therefore never consumes `template/base/Directory.Packages.props`, so any value
   of `<FsGgUiVersion>` — coherent or stale — cannot redden the repo build. (`grep -l template/base
   FS.GG.Rendering.slnx` ⇒ not referenced.)
2. **No existing coherence step:** before this feature, `.github/workflows/gate.yml` had **no** step
   that reads `<FsGgUiVersion>` against the tags/BOM/template manifest. Drift only surfaced downstream
   when a consumer scaffolded a product (Feature 204, `FS-GG/FS.GG.Rendering#1`).

Together these prove the "drift is silent" premise the plan flagged as unverified. This feature's new
gate step is what makes the drift loud and local.

`grep` confirmation of D3 (single literal, all pins derive): `grep -c '<FsGgUiVersion>'` ⇒ `1`; no
`FS.GG.UI.*` template pin carries a hardcoded literal (every one is `$(FsGgUiVersion)`). No stray
hardcoded pin exists — the verify-only design (D3) holds; no propagation script is needed.

## T005 — early smoke: today's tree is coherent & restorable

The live pack→restore evidence (pack the 16 `FS.GG.UI.*` members + BOM from source at the pinned `V`
to a throwaway feed, restore `FS.GG.UI@V` in a clean consumer, assert all 16 resolve to exactly `V`)
is produced by the guard's own restore-grounded proof layer
(`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx`). The
early-smoke confirmation and the US3 live-layer evidence are the **same** real pack/restore — see
`version-coherence.md` (`provenance: live`, `resolved-members-at-version: 16/16`). Consolidated to one
real restore rather than two identical packs; disclosed here so the early-smoke step is not mistaken
for a separate run.
