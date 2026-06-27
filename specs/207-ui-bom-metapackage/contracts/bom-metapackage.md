# Contract: The BOM Metapackage Artifact (US1/US3 — FR-001, FR-005, FR-006, FR-009)

Defines what the published `FS.GG.UI` metapackage **is** and how it is produced so its version maps
1:1 to a reproducible coherent snapshot.

## BM-1 — Identity & shape

- Package ID: **`FS.GG.UI`** (bare brand root; free per research R2).
- Ships **no assembly** (`IncludeBuildOutput=false`) — dependencies only.
- Carries exactly **16** dependencies, one per coherent member (data-model E2).

## BM-2 — Exact-version dependencies

Every dependency is pinned **exactly** (`[$version$]`), e.g.:

```xml
<!-- FS.GG.UI.nuspec (excerpt) -->
<dependencies>
  <dependency id="FS.GG.UI.Build"           version="[$version$]" />
  <dependency id="FS.GG.UI.Scene"           version="[$version$]" />
  <!-- … all 16 … -->
  <dependency id="FS.GG.UI.Themes.Default"  version="[$version$]" />
</dependencies>
```

`[$version$]` is the **single** version token; `$version$` is supplied by
`-p:NuspecProperties=version=$(Version)` at pack time. There is no literal per-member version.

## BM-3 — Single-source version derivation (FR-009)

- The metaproject sets `NuspecFile=FS.GG.UI.nuspec` and
  `NuspecProperties=version=$(Version)`; `$(Version)` comes from the one
  `dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=V`.
- The BOM's own package version and all 16 dependency versions are therefore the **same** `V`.

## BM-4 — Same-snapshot publication (FR-006) & channel (FR-005)

- The `src/Meta` project is in `FS.GG.Rendering.slnx`, so the existing one-command pack produces
  `FS.GG.UI@V` **with** the 16 members at `V`.
- The snapshot is tagged `fs-skia-ui/v<V>` (feature 204 mechanism), now including `FS.GG.UI@V`.
- Channel follows `V` automatically: `-preview.N` ⇒ preview; bare `x.y.z` ⇒ stable.

## Pass conditions

| ID | Condition | Maps to |
|----|-----------|---------|
| BM-A | Packing the snapshot at `V` produces `FS.GG.UI@V` with 16 `[V]` member deps and no `lib/`. | FR-001, FR-009 |
| BM-B | `FS.GG.UI@V` is in the same packed feed/tag as the members at `V`. | FR-006 |
| BM-C | `FS.GG.UI@V` channel == members' channel (same `V`). | FR-005 |
| BM-D | No second FS.GG.UI version literal exists anywhere the feature adds (one `$version$`). | FR-009, SC-005 |
