# Contract: `fs-gg-ui-template` release delta — `game` profile UNRELEASED → released

**Contract id**: `fs-gg-ui-template` (owner `rendering`; consumers `templates`, `sdd`)
**Registry**: `FS-GG/.github` → `registry/dependencies.yml` (+ `docs/registry/compatibility.md` projection)
**Change kind**: `contract-change` — advances released-coordinates + flips the `game`-profile release-state +
coherence. **Surface-additive** (the `game` profile already shipped in Feature 220, commit `b78e72a`);
**no parameter/protocol surface changes** (FR-010).

This is not an F# API contract (no `.fsi`). It is the cross-repo packaging/coordination contract: what the
org feed must serve, with what contents, and how the registry must read afterward. Package readability was
already resolved org-wide in Feature 218 and is **not** re-touched here (see §5).

## 1. Producer obligation — published coordinates (resolves #33)

| Field | Before | After (this release) |
|---|---|---|
| `version` (template `FsGgUiVersion` pin) | `0.1.53-preview.1` | `V` (= `0.1.54-preview.1`, `> 0.1.53-preview.1`) |
| `package-version` (PUBLISHED `FS.GG.UI.Template`) | `0.1.53-preview.1` | `V` |
| `package-tag` (template-scoped coherent-set tag) | `fs-gg-ui-template/v0.1.53-preview.1` | `fs-gg-ui-template/v<V>` |
| `game` profile feed-state | "UNRELEASED until the next `fs-gg-ui-template` republish" | "released in `V`" |

**Acceptance**:
- `gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template/versions --jq '.[].name'` includes `V > 0.1.53-preview.1`. (SC-002)
- **Content gate** — the released tag carries Feature 220: `git merge-base --is-ancestor b78e72a fs-gg-ui-template/v<V>` is true, and the packed `V` template exposes the `game` choice (content inspection, not just version string). (FR-002, SC-002)
- The packed `V` is `game`-scaffold-selectable: scaffolding with the `game` profile selected exits 0 with the minimal Pong-style MVU starter generated — no missing-profile / unknown-choice error. (FR-004, SC-001)
- The whole coherent set (every `FS.GG.UI.*` + the template) is served at the same `V`. (FR-001)

## 2. Producer obligation — consumer reachability (carried forward from Feature 218)

| Field | Before | After |
|---|---|---|
| `FS.GG.UI.Template` package `visibility` | org-readable (`internal`, set in Feature 218) | unchanged — org-readable persists across versions |

**Acceptance**:
- An ordinary org-consumer token (`packages: read`, no special grant) runs `dotnet new install FS.GG.UI.Template::V` at exit 0, not 103. (FR-003, SC-001)

> **Mechanism note**: GitHub Packages visibility is per-package and persists across versions, so the
> Feature-218 org-readability applies to `V` with no new action. This feature performs **no** visibility
> change; the live probe re-confirms no exit 103 as a safety net (research R4). The "re-privatized" edge
> case is caught by the probe, not pre-empted by a flip.

## 3. Registry landing (FR-006, ADR-0001) — publish-BEFORE-flip

A `contract-change` PR against `FS-GG/.github` updates the `fs-gg-ui-template` entry per §1
(version / package-version / package-tag → `V`; `game`-profile note **UNRELEASED → released @ V**) and
flips its coherence block (`- id: fs-gg-ui-template`): `resolved_by` advances to `fs-gg-ui-template/v<V>`.
The `docs/registry/compatibility.md` projection is regenerated to match (no stale `0.1.53-preview.1` for this
surface). No coherence flag flips `true→false` — the contract stays coherent; this *advances* it.

> **Ordering (FR-007)**: the flip MUST follow a **confirmed feed listing** of `V` (§1 acceptance green).
> The registry must never read "released" for a version the feed 404s. The prior registry PR
> (FS-GG/.github#77) already *recorded* `game` as UNRELEASED; this PR flips that note.

## 4. Downstream (consumer half — confirmed/notified, not built here)

- **SDD#44** (`app → game` default-selection flip, owned by the SDD scaffold-provider) is **notified** of
  the published `V` via the existing dispatch-sender / a cross-repo comment so the default flip can proceed.
  This feature does **not** perform the default-selection change (spec Assumption, FR-009).
- **Board / issues** — #33 is closed with `V` + the registry PR link; board item #33 → `Done`; the
  `Blocked by: FS.GG.Rendering#33` mirror on item #31 is cleared (FR-008). This feature confirms/links the
  unblock; the consumer feedback resolution on #31 itself is consumer-owned.

## 5. Non-obligations (explicit scope fence)

- No change to any scaffold parameter semantics or the scaffold-provider invocation protocol (FR-010).
- No new `FS.GG.UI.*` package, no package removal, no framework `net10.0` change, no new `.fs`/`.fsi`.
- No `release.yml` / `derive-template-version.sh` / dispatch-sender logic change — they are already
  authored; this feature *exercises* them (FR-010).
- No package-visibility action (resolved in Feature 218; §2).
- The `app → game` default-selection flip is out of scope (SDD#44).
