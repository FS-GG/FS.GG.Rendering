# Cross-repo resolution evidence — US3 (XR-A..XR-E, SC-004/SC-005)

Performed **only after** US1 (four profiles green) and US2 (reproducible + tagged) held (FR-007 gate).

## Resolution commit / tag (this repo)

- Resolution commit on `main`: **`57be86c`** (`204: restore fs-skia-ui-version cross-repo coherence …`).
- Immutable tag: **`fs-skia-ui/v0.1.50-preview.1`** → `57be86c`, pushed to `origin`.

## Registry flip — `FS-GG/.github` @ `942d91e` (XR-A, XR-B)

- `registry/dependencies.yml`: `fs-skia-ui-version` → **`coherent: true`** with
  `resolved_by: FS-GG/FS.GG.Rendering@57be86c — tag fs-skia-ui/v0.1.50-preview.1`; the
  `fs-gg-ui-template` contract version bumped `0.1.0-preview.1` → `0.1.50-preview.1`.
- `docs/registry/compatibility.md` (projection): `fs-skia-ui-version` → **✅ yes**, summary matches
  the authoritative row; the `fs-gg-ui-template` row version updated to `0.1.50-preview.1`; the
  explanatory prose moved to past tense (resolved). Both files changed **together**.

Verified live: `gh api repos/FS-GG/.github/contents/registry/dependencies.yml` → `coherent: true`.

## Request issue — `FS-GG/FS.GG.Rendering#1` (XR-C)

- `## Response` comment posted (option taken, pin, tag, four-profile green, phantom-pin removal,
  evidence links): comment `#issuecomment-4817510498`.
- Issue **CLOSED**.

## No stale signal (XR-D, XR-E / SC-005)

- Issue #1 final state: `state=CLOSED`, labels `[cross-repo, cross-repo:request]` — **`blocked` removed**.
- Registry row `coherent: true`; projection agrees; no `0.1.0-preview.1` left in the
  `fs-skia-ui-version` / `fs-gg-ui-template` registry entries.
- The flip/close happened strictly **after** the US1/US2 evidence (XR-D).

**US3 holds — the coherence loop is closed.**
