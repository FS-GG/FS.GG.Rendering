# Phase 1 Data Model: Optional FS.GG.UI BOM / Metapackage

This feature ships a packaging artifact, not runtime data; the "entities" are the package/version
objects and the invariants that bind them. They are derived from the spec's Key Entities and the
research decisions.

## Entities

### E1 — BOM metapackage (`FS.GG.UI`)

The new published artifact; the consumer-facing "one reference" surface.

| Field | Value / Rule |
|-------|--------------|
| Package ID | `FS.GG.UI` (bare brand root; currently unused — R2) |
| Version | `V` — equals the coherent snapshot version (R4); carries channel in the value |
| Build output | none (`IncludeBuildOutput=false`) — dependencies only, no assembly |
| Dependencies | the 16 members (E2), each at **exact** `[V]` (R1) |
| Produced by | `src/Meta/FS.GG.UI.metaproj` packing `FS.GG.UI.nuspec` (`version=$(Version)`) |
| Packed in | the same `dotnet pack FS.GG.Rendering.slnx -p:Version=V` as the members (R4) |

**Validation rules**: ships no `lib/`; exactly 16 `<dependency>` entries; every dependency version
is the literal `[$version$]` token (no per-member literal); resolves to a complete coherent set on
restore.

### E2 — Coherent FS.GG.UI member set (16 packages)

The co-versioned framework packages the BOM aggregates; membership is exactly feature 204's snapshot
manifest set.

| Field | Value / Rule |
|-------|--------------|
| Members | `Build, Scene, Canvas, Controls, Controls.Elmish, DesignSystem, Diagnostics, Elmish, KeyboardInput, Layout, SkiaViewer, Symbology, Symbology.Render, Testing, Themes.AntDesign, Themes.Default` |
| Excluded | `ColorPolicy` (`IsPackable=false`); `FS.GG.UI.Template` (template, not runtime); phantom `Color`/`SkillSupport` (retired) |
| Source of truth | the `IsPackable=true` `FS.GG.UI.*` projects under `src/**` |

**Validation rules (parity invariant — R3)**: `{ BOM dependency IDs } == { packable FS.GG.UI.* project IDs }`.
A member added/removed without a matching nuspec edit violates this and fails the parity test.

### E3 — Release snapshot / tag

The immutable snapshot (feature 204 mechanism) the BOM version `V` names.

| Field | Value / Rule |
|-------|--------------|
| Tag | `fs-skia-ui/v<V>` (annotated) at the resolution commit |
| Contents | the 16 members **and** `FS.GG.UI` (E1), all at `V` |
| Reproducibility | re-checkout + re-pack reproduces the identical set; clean restore is byte-identical across runs (SC-004) |

### E4 — Cross-repo compatibility registry row (`fs-skia-ui-version`)

The cross-repo record (in `FS-GG/.github`) acknowledging the BOM as part of the coherent set.

| Field | Value / Rule |
|-------|--------------|
| Location | `FS-GG/.github`: `registry/dependencies.yml` + `docs/registry/compatibility.md` |
| Update | records `FS.GG.UI` BOM as part of the coherent `FS.GG.UI` set, under/alongside `fs-skia-ui-version` |
| Gate | written **only after** US1+US2 verified (FR-008); via `gh`, not files in this repo |

### E5 — Clean consumer fixture (verification only)

A throwaway project proving the behavior; not a shipped artifact.

| Field | Value / Rule |
|-------|--------------|
| Declaration | a single `PackageReference Include="FS.GG.UI" Version="V"` — no other FS.GG.UI literal |
| Pass (US1) | restore+build; every resolved `FS.GG.UI.*` is at `V`; no NU1101/NU1605/NU1608 |
| Pass (US2) | adding a member at `Y≠V` ⇒ restore/build conflict (NU1605/NU1107); no mixed graph |

## Cross-entity invariants

- **INV-1 (single version — FR-009)**: `E1.Version == every E1.dependency version == E3.tag version`.
  One literal (`V`), one place (`-p:Version=V`); no second version anywhere.
- **INV-2 (channel match — FR-005)**: `E1.Version` channel == members' channel (automatic: same `V`).
- **INV-3 (completeness — FR-003)**: `E1.dependencies` covers all of `E2` (parity invariant E2).
- **INV-4 (loud deviation — FR-004)**: any member resolved at `≠ E1.Version` is impossible without a
  restore/build conflict (exact `[V]`).
- **INV-5 (optionality — FR-007)**: the `fs-gg-ui` template's `FsSkiaUiVersion`/CPM surface is
  unchanged; the BOM is additive.
- **INV-6 (gated record — FR-008)**: `E4` is written only after E5 passes US1+US2.

## State / sequencing

```
pack coherent snapshot (members + FS.GG.UI @ V)  →  tag fs-skia-ui/v<V>
        │
        ▼
clean-consumer restore+build @ V  ──(US1 pass)──┐
forced member Y≠V ⇒ conflict      ──(US2 pass)──┤
restore twice ⇒ identical set     ──(US3 evidence)──┘
        │  (only if US1+US2 pass)
        ▼
record FS.GG.UI BOM in fs-skia-ui-version registry (FS-GG/.github, via gh)
```
