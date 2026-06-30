# T024 — Evidence summary (Feature 222)

Every live artifact mapped to its Success Criterion / Functional Requirement. Published version
**`V = 0.1.54-preview.1`**; release run [`28468936061`](https://github.com/FS-GG/FS.GG.Rendering/actions/runs/28468936061); registry PR [FS-GG/.github#78](https://github.com/FS-GG/.github/pull/78).

## Critical path (publish → selectable → flip → close)

| Gate | Evidence file | SC / FR | Result |
|---|---|---|---|
| Pre-publish gap confirmed live | `pre-publish-probe.md` | (root cause) | feed served 0.1.53 **without** `b78e72a`; `main` had it |
| Machinery intact, no edits | `machinery-check.md` | FR-010 | release.yml + `game` choice unchanged |
| Pins bumped → release cut | `publish.md` | FR-001 | both pins `0.1.53 → 0.1.54-preview.1`; tag-set pushed |
| Dispatch propagated `V` | `publish.md` | FR-010 | `template-dispatch` `28468936457` ✅ |
| Feed serves `V` | `feed-listing.md` | SC-002 | `0.1.54-preview.1` served; 18 pkgs pushed |
| Coherent set @ `V` | `coherent-set.md` | FR-001, SC-002 | template == Scene/Build/Controls/UI @ 0.1.54 |
| Content gate (carries F220) | `content-gate.md` | FR-002, SC-002 | `b78e72a` ancestor of `…/v0.1.54-preview.1` |
| Consumer install, no 103 | `consumer-install.md` | FR-003, SC-001 | `dotnet new install …@0.1.54` exit 0 |
| `game` scaffold-selectable | `game-scaffold.md` | FR-004, SC-001 | `--profile game` accepted → Pong MVU starter |
| `game` builds + governance green, zero edits | `game-governance.md` | FR-004, SC-003 | build 0/0; governance **26/26**, zero `GovernanceTests` edits |
| Non-game profiles unaffected | `non-game-parity.md` | FR-005, SC-003 | headless-scene/governed/sample-pack byte-identical to F220; `app` → controls showcase |
| Registry flip (publish-before-flip) | `registry-pr.md` | FR-006/007, SC-004 | PR #78 merged; coherence gate green; `game` released @ `V` |
| #33 closed, board Done, #31 clear, SDD#44 notified | `board-closure.md` | FR-008/009, SC-005 | #33 CLOSED + recorded; board Done; #31 unblocked; SDD#44 notified |

## Regression

| Artifact | Result |
|---|---|
| `baseline.md` (T002, pre) | 21 projects · 7 green / 14 red — all 14 are pre-existing NU1403 restore failures + the known `Package.Tests` Build-engine red (not regressions; this feature has **zero** `src/.fs` change) |
| `baseline-post.md` (T027) | re-baseline diffs to **zero new reds** vs T002 (see T027) |

## Disclosed substitutes (Principle V — none faked)

- **Consumer token**: ran as org member `EHotwagner`, not a separate `packages: read`-only token —
  sound because the `FS.GG.UI.*` packages are org-**public** (Feature 218), so 0.1.54 is consumer-readable
  (exit-103 cannot occur for a public package). The local repo template was uninstalled so the **feed**
  package is the unambiguous source for the scaffold/build/governance probes.
- **Operator-gated steps** (tag push, registry-PR merge): operator rights were present — **not** deferred.
