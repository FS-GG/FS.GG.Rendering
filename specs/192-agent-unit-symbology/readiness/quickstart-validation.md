# Quickstart validation (T041) — M0–M5 coverage / SC-001…SC-009

All checks are real runs in this checkout on 2026-06-25 (no synthetic/substitute/degraded evidence).

| SC | Criterion | Milestone | Evidence | Result |
|---|---|---|---|---|
| SC-001 | determinism / stable identity | M1 | `Symbology.Tests` `DeterminismTests` (equal Scene, byte-equal canonical bytes, stable gallery identity); M5 golden board re-exports byte-identically (`golden-identity.txt`, `byte-stable: true`) | ✅ PASS |
| SC-002 | channel presence (each channel observably alters output) | M1 | `ChannelPresenceTests` — 10 channels, each pair differs in canonical-bytes identity | ✅ PASS |
| SC-003 | codec fidelity | M1 | `CodecFidelityTests` — export→import→re-export byte-identical; Path/Arc/Ellipse(gradient) kinds preserved; radial/linear/sweep gradient round-trip guard | ✅ PASS |
| SC-004 | no core-surface drift | M1/M2/M3 | `git status` shows only the two NEW baselines added; `scripts/refresh-surface-baselines.fsx` leaves `Scene`/`SkiaViewer`/`Controls`/`Canvas`/… baselines unchanged | ✅ PASS |
| SC-005 | skill parity | M4 | `scripts/check-agent-skill-parity.fsx --fail-on high` → status passed, critical=0 high=0 warning=0 | ✅ PASS |
| SC-006 | filmstrip reproducibility | M2 | `FilmstripTests` — `filmstrip` byte-reproducible across builds; sample count drives frames; phase from schedule (no clock) | ✅ PASS |
| SC-007 | legibility at target size | M1 (+ M5) | channel separability proven via canonical identity at the board size; M5 dry-run rendered real ~25 KB boards critiqued at the target on-board size (`design-rationale.md`) | ✅ PASS |
| SC-008 | public render pass + fail-loud | M3 | `Symbology.Render.Tests` `RenderPassTests` (non-blank PNG, ReferencePassed, content-stable path) + `RenderFailLoudTests` (raises on non-pass) | ✅ PASS |
| SC-009 | dry-run audit trail | M5 | `readiness/dry-run/` — 3 rounds × (timestamped board PNG + mapping snapshot), golden board + identity, final symbol-set module + rationale | ✅ PASS |

## Milestone coverage

- **M0** render-bridge spike → `m0-spike-evidence.md` (ReferencePassed, non-blank PNG; FSI native-load caveat disclosed).
- **M1** pure `FS.GG.UI.Symbology` (`token`/`gallery`) → `Symbology.Tests` 34/34 green; baseline pinned.
- **M2** motion + boards (`animate`/`filmstrip`) → included in the 34 green; byte-reproducible.
- **M3** render bridge `FS.GG.UI.Symbology.Render` (`Render.toPng`) → `Symbology.Render.Tests` 3/3 green; baseline pinned; packs + restores from local feed.
- **M4** `fs-gg-symbology` skill (×3 trees + canonical + reference `.fsx`) → parity green.
- **M5** end-to-end dry-run with provenance → `dry-run/` audit trail + golden board.

## Test totals (this feature)

- `tests/Symbology.Tests` → **34 passed / 0 failed**
- `tests/Symbology.Render.Tests` → **3 passed / 0 failed**
