# T018–T020 — Registry flip UNRELEASED → released (FR-006/007, SC-004)

**Landed only after the feed listing (T011–T013) was green — publish-before-flip (FR-007).**

## Contract-change PR

- **PR**: [FS-GG/.github#78](https://github.com/FS-GG/.github/pull/78) — `contract-change` label.
- **Coherence gate**: `contract-coherence / coherence` ✅ pass (21s, run `28469581521`) — the typed
  `fsgg-sdd registry validate` accepts the advanced registry; fsgg-contracts pin + build-config drift
  stay coherent.
- **Merged**: `2026-06-30T19:13:40Z`, squash commit `733f148` to `main`.

## T018 — `registry/dependencies.yml` (fs-gg-ui-template)

- `version` / `package-version` / `package-tag` → **0.1.54-preview.1** /
  `fs-gg-ui-template/v0.1.54-preview.1`.
- `profiles.game.added`: **UNRELEASED → RELEASED @ 0.1.54-preview.1** (Feature 222, #33).
- dependency edge `templates → rendering`: `fs-gg-ui-template@0.1.54-preview.1`.
- coherence `id: fs-gg-ui-template`: `resolved_by → fs-gg-ui-template/v0.1.54-preview.1` + a
  Feature-222 advance paragraph; the top `updated:` note prepends the Feature-222 entry and flips the
  F220 "UNRELEASED" note.
- **No coherence flag flips `true → false`** — this advances a coherent contract.

## T019 — `docs/registry/compatibility.md` projection (regenerated)

- Edge diagram: `fs-gg-ui-template@0.1.54-preview.1 … game-profile-released`.
- Contract row + coherence-state cell name `0.1.54-preview.1` and read `game` **released** (live FR
  evidence inline). No stale `0.1.53-preview.1` for the current-state of this surface (historical
  "prior 0.1.53" narrative retained as history).

## T020 — Land

Opened + landed the `contract-change` PR **only after** T011/T012/T013 were green (publish-before-flip).
Operator merge rights present (admin on `FS-GG/.github`) — not deferred.
