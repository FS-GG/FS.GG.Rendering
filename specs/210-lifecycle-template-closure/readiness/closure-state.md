# Closure State — Feature 210 (T019)

Board: **FS-GG Projects v2 "Coordination"** (`PVT_kwDOEYAWY84Bb08W`).
Epic item: `P1 · Epic — Make fs-gg-ui emit Spec Kit only when asked (lifecycle-agnostic template)`
(`PVTI_lADOEYAWY84Bb08WzgxBbC0`, draft `DI_lADOEYAWY84Bb08WzgKrVHg`).

## Board transition applied

| field | before | after |
|---|---|---|
| Status (P1 Epic) | `Ready` (stale) | `In review` — Rendering-side complete, live acceptance evidence delivered, awaiting final epic sign-off |

`gh project item-edit --project-id PVT_kwDOEYAWY84Bb08W --id PVTI_lADOEYAWY84Bb08WzgxBbC0 \`
`  --field-id PVTSSF_lADOEYAWY84Bb08WzhWih5w --single-select-option-id 7b8fd019` (Status → In review)

## Rendering-side vs epic-fully-done

- **Rendering-side**: **complete**. The published `FS.GG.UI.Template 0.1.51-preview.1` package was
  validated live (3 lifecycle × 4 profile matrix + byte-identical default + build spot-check `pass`);
  see `epic-acceptance.md`. All P1 `rendering` child board items are `Done`.
- **epic-fully-done**: **achievable now** — no open cross-repo remainder remains (see below). The
  epic is set `In review` rather than `Done` to leave the final multi-repo sign-off to the Coordinator;
  the invariant "epic_fully_done = false while any remainder is open" holds vacuously (none open).

## Cross-repo remainder — attribution & state (each tracked exactly once)

| item | owning repo | tracker (single) | state |
|---|---|---|---|
| Scaffold-path git-init/chmod obligations | FS-GG/FS.GG.SDD | issue [#1](https://github.com/FS-GG/FS.GG.SDD/issues/1) + board item `PVTI_…WzgxB7qM` | **Done** (issue CLOSED 2026-06-27) |
| Constitution ownership for `lifecycle=sdd` | FS-GG (cross-repo decision) | board draft decision `DI_lADOEYAWY84Bb08WzgKrVHM` | **Done** (downstream P2 impl also Done) |

### Dedupe note (FR-010, SC-005)

The constitution-ownership decision was **already tracked once** as the board P0 decision item (Done).
A fresh SDD issue (`FS-GG/FS.GG.SDD#2`) was opened during this work and then **closed as a duplicate**
of that canonical board item — so the invariant "exactly one tracked item per remainder, no
duplicates" holds. The scaffold-path obligation likewise has exactly one tracker (SDD#1 + its board
mirror), already resolved.

## Result

The Coordination board is honest: the P1 epic shows Rendering-side complete (`In review`), every
remainder item is attributed to its owning repo with a single tracker, and all remainder items are
`Done` — so nothing silently blocks full closure.
