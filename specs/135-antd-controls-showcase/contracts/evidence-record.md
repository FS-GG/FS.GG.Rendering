# Contract: Per-Page Evidence Record

Reused from G1 (`ControlsGallery.Core.Evidence`), Ant-themed. Written by `AntShowcase.App evidence --seed N`
to `artifacts/ant-showcase/<seed>/<page-id>/`.

## Files per page

| File | Content |
|---|---|
| `run.json` | structured record: `PageId`, `Seed`, `Mode` (`antLight`/`antDark`), `ControlIds` (for Catalog pages), the final-state digest, the screenshot-evidence result fields, and `NotAuthoritativeFor` |
| `state.txt` | deterministic textual dump of the final `Model`/`PageState` after the seeded script |
| `summary.md` | human-readable per-page summary incl. the disclosure |
| `frame.png` | offscreen screenshot of the page's required surfaces (present when GL/offscreen available) |

## Required fields & rules

- **`NotAuthoritativeFor`** — non-empty disclosure of what the run does **not** prove (FR-012). E.g.
  "does not prove pixel-level Ant fidelity vs upstream antd; does not exercise live pointer hit-testing
  beyond the seeded script; chart/graph controls rendered with seeded sample data only."
- **Degrade-and-disclose** (FR-013 / SC-005) — when no display/GL: `ProvesScreenshot=false`,
  `UnsupportedHostReason`/`BlockedStage` populated from `ScreenshotEvidenceResult`, exit 0, no hang, no
  `frame.png` fabricated.
- **Determinism** (FR-011 / SC-004) — same `--seed` ⇒ byte-identical `run.json` + `state.txt` across two
  runs. The record carries no wall-clock timestamp in its identity-bearing fields (any timing is an
  advisory artifact, not part of the byte-compared payload).
