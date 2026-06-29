# Contract: `fs-gg-ui-template` release & readability delta

**Contract id**: `fs-gg-ui-template` (owner `rendering`; consumers `templates`, `sdd`)
**Registry**: `FS-GG/.github` → `registry/dependencies.yml` (+ `docs/registry/compatibility.md` projection)
**Change kind**: `contract-change` — released-coordinates + coherence + consumer-readability. **Surface-additive**
(`productName` already specified in Feature 217); **no parameter/protocol surface changes** (FR-009).

This is not an F# API contract (no `.fsi`). It is the cross-repo packaging/coordination contract: what the
org feed must serve, at what reachability, and how the registry must read afterward.

## 1. Producer obligation — published coordinates (resolves #29)

| Field | Before | After (this release) |
|---|---|---|
| `version` (template `FsGgUiVersion` pin) | `0.1.52-preview.1` | `V` (= `0.1.53-preview.1`, `> 0.1.52-preview.1`) |
| `package-version` (PUBLISHED `FS.GG.UI.Template`) | `0.1.52-preview.1` | `V` |
| `package-tag` (template-scoped coherent-set tag) | `fs-gg-ui-template/v0.1.52-preview.1` | `fs-gg-ui-template/v<V>` |
| `productName` parameter feed-note | "UNRELEASED on the feed (lands next release)" | "released in `V`" |

**Acceptance**:
- `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` includes `V`. (SC-001)
- The packed `V` template honors `--productName`: `dotnet new fs-gg-ui --productName <P>` exits 0, not 127. (SC-003, INV-6)
- The whole coherent set (every `FS.GG.UI.*` + the template) is served at the same `V`. (FR-006, INV-2)

## 2. Producer obligation — consumer readability (resolves #26)

| Field | Before | After |
|---|---|---|
| `FS.GG.UI.Template` package `visibility` | `private` | `internal` (org-wide read) — **or** Templates repo Read grant (fallback) |

**Acceptance**:
- `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility` == `internal` (or the Templates Read grant exists). (INV-8)
- An ordinary org-consumer token (`packages: read`, no special grant) runs `dotnet new install FS.GG.UI.Template@V` at exit 0, not 103. (SC-002)

> **Mechanism note**: GitHub exposes **no REST endpoint** to change package visibility; this is an admin UI
> action (`orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`). It is independent of the publish —
> visibility is per-package and persists across versions (research R3). Both obligations (§1, §2) must hold
> for the **same `V`** (FR-004) for the contract to be satisfied.

## 3. Registry landing (FR-008, ADR-0001)

A `contract-change` PR against `FS-GG/.github` updates the `fs-gg-ui-template` entry per §1 (version/
package-version/package-tag → `V`; productName note → released) and its coherence block
(`- id: fs-gg-ui-template`, `coherent: true`): `resolved_by` advances to `fs-gg-ui-template/v<V>`, recording
org-readability so the Templates-CI consumer half is no longer auth-blocked. The compatibility projection is
regenerated to match. No coherence flag flips `true→false` (the contract stays coherent; this *advances* it).

## 4. Downstream (consumer half — Templates-owned, confirmed not built here)

Per #29's "Downstream (we own, after publish)": FS.GG.Templates re-pins to `V`
(`scripts/bump-rendering-pin.sh <V>`), runs `FSGG_COMPOSITION_FULL=1 tests/composition/run.sh` → `29/29`
(SC-004), and FS.GG.Templates#32 moves its pin. This feature **confirms** that unblock and links it; it does
not perform the Templates re-pin (spec Assumption).

## 5. Non-obligations (explicit scope fence)

- No change to the `name`/`productName` parameter semantics or the scaffold-provider invocation protocol (FR-009).
- No new `FS.GG.UI.*` package, no package removal, no framework `net10.0` change.
- No `release.yml` / `template-dispatch.yml` logic change — they are already authored; this feature *exercises* them.
