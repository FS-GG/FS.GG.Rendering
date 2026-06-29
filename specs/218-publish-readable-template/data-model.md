# Phase 1 Data Model: Publish & Make-Readable the productName-Enabled Template

This feature has no application datastore. The "model" is the set of **coordination entities** whose
state this feature transitions, plus the invariants binding them. Entities map directly to spec Key
Entities and the FR-### they satisfy.

## Entity: Coherent-set version `V`

- **What**: the single version string every `FS.GG.UI.*` package and the template share for one release.
- **Fields**: `value` (e.g. `0.1.53-preview.1`), `predecessor` (`0.1.52-preview.1`).
- **Invariants**:
  - `value > predecessor` strictly (FR-001). [INV-1]
  - The two in-repo pins agree with `value`: `template/base/Directory.Packages.props` `<FsGgUiVersion>`
    **and** `.template.package/FS.GG.UI.Template.fsproj` `<Version>` both equal `value` (FR-006). [INV-2]
- **State transitions**: `0.1.52-preview.1 (current)` → *(merge bump)* → `0.1.53-preview.1 (pinned in repo)`
  → *(tag push + release.yml)* → `served on org feed`.

## Entity: Release tag-set

- **What**: the three git tags a complete coherent-set release pushes at `V` (research R2).
- **Fields**: `publish-tag = v<V>`, `dispatch-tag = fs-gg-ui-template/v<V>`, `snapshot-tag = fs-gg-ui/v<V>`.
- **Invariants**:
  - `publish-tag` present ⇒ `release.yml` publishes the set (necessary for SC-001). [INV-3]
  - `dispatch-tag` present ⇒ `template-dispatch.yml` notifies Templates (FR-010, SHOULD). [INV-4]
  - `snapshot-tag` present ⇒ the Feature-209 coherence mirror in `Package.Tests` can re-derive its verdict. [INV-5]
- **State transitions**: `absent` → `pushed` → `release/dispatch workflows triggered` → `feed/Templates updated`.

## Entity: Template package artifact

- **What**: `FS.GG.UI.Template` `V`, packed from `.template.package/FS.GG.UI.Template.fsproj` (which includes
  the repo-root `.template.config/template.json` + template content via `..\**\*`).
- **Invariants**:
  - Carries Feature 217: installing `V` and running `dotnet new fs-gg-ui --productName <P>` (no `-n`) exits 0,
    **not** 127 (FR-002, SC-003). [INV-6]
  - Absent `productName` ⇒ byte-identical to the pre-217 output (Feature 217's own guarantee; not re-proven here). [INV-7]
- **State transitions**: `built locally` → `packed at V` → `pushed to org feed` → `served`.

## Entity: Package visibility

- **What**: the org package-settings reachability flag on `FS.GG.UI.Template`.
- **Fields**: `visibility ∈ {private, internal, public}`, `repository = FS-GG/FS.GG.Rendering`.
- **Invariants**:
  - After this feature, an ordinary org-consumer token (`packages: read`, no special grant) installs `V`
    with `dotnet new install` at exit 0, **not** 103 (FR-003, SC-002). Satisfied by `visibility = internal`
    (preferred) **or** an explicit `FS-GG/FS.GG.Templates` repo Read grant. [INV-8]
  - Per-package and version-independent: the flag persists across published versions (research R3) — so
    INV-8 is **not** a side effect of publishing `V`; it is a distinct transition. [INV-9]
- **State transitions**: `private (current)` → *(admin UI action — no REST)* → `internal (readable)`.

## Entity: `fs-gg-ui-template` registry record (`FS-GG/.github`)

- **What**: the cross-repo contract entry in `registry/dependencies.yml` + the `docs/registry/compatibility.md`
  projection.
- **Fields**: `version`, `package-version`, `package-tag`, the `productName` parameter feed-note, and the
  coherence block (`- id: fs-gg-ui-template`, `coherent: true`, `resolved_by`).
- **Invariants**:
  - After resolution, `version = package-version = V`, `package-tag = fs-gg-ui-template/v<V>` (FR-008). [INV-10]
  - The `productName` feed-note reads **released in `V`** (no longer "UNRELEASED on the feed"). [INV-11]
  - The coherence entry's `resolved_by` advances to `fs-gg-ui-template/v<V>` and records org-readability. [INV-12]
  - No contract *surface* field changes — only released coordinates + coherence (FR-009). [INV-13]
- **State transitions**: `pinned 0.1.52-preview.1 / productName UNRELEASED` → *(contract-change PR on `.github`)*
  → `pinned V / productName released / readable`.

## Entity: Cross-repo requests & board items

- **What**: FS-GG/FS.GG.Rendering **#29** (publish), **#26** (visibility), their two Coordination-board rows
  (Phase P4 Templates · Workstream Composition · Contract `fs-gg-ui-template`), and the downstream
  FS.GG.Templates **#32** (pin bump) they unblock.
- **Invariants**:
  - #29 carries a `## Response` with the published `V` string (FR-007). [INV-14]
  - #29 **and** #26 are `closed` only once **both** INV-6 and INV-8 hold for the *same* `V` (FR-004 — no
    half-landing). [INV-15]
  - Both board rows move to `Done`; #32 becomes unblocked (its `Blocked by` references #29/#26 cleared). [INV-16]
- **State transitions**: `#29/#26 Ready (open)` → `V published + readable` → `responded + closed` → `board Done`
  → `#32 unblocked`.

## The binding invariant (FR-004 — no half-landing)

There MUST exist **one** `V` for which **both** hold simultaneously:

```
INV-6  (V honors --productName → no exit 127)
   AND
INV-8  (V is readable by an org consumer token → no exit 103)
```

Publishing `V` while still `private` satisfies INV-6 but not INV-8 (→ 103). Flipping visibility while only
`0.1.52` is on the feed satisfies INV-8 but not INV-6 (→ 127). Neither alone closes #29+#26 or unblocks #32.
