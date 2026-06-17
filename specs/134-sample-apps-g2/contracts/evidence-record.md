# Contract — Per-sample Evidence Record

Extends G1's package-only evidence schema (`samples/ControlsGallery/ControlsGallery.Core/Evidence.fs`) with
a sample `Outcome` block. Written per sample under `artifacts/sample-apps/<seed>/<sample-id>/`. Hand-rolled,
fixed field order ⇒ **byte-stable** (no `System.Text.Json`, no wall-clock fields). Research R5.

## Files written

| File | Content |
|---|---|
| `run.json` | the full record (below), byte-stable |
| `summary.md` | human-readable disclosure (proof level, authoritative / **NOT** authoritative, outcome, screenshot status) |
| `state.txt` | golden `FrameMetrics` — **count/bool fields only**, no `*Duration` (timing excluded) — as G1 |
| `frame.png` | the offscreen screenshot **iff** `screenshot.provesScreenshot` (else absent; never stale) |

## `run.json` shape

```json
{
  "sampleId": "tetris",
  "seed": 7,
  "proofLevel": "deterministic",
  "authoritativeFor": ["determinism", "tree-equality", "outcome", "non-blank-offscreen-png"],
  "notAuthoritativeFor": ["renderer-vs-desktop-pixels", "live-host", "timing"],
  "outcome": {
    "kind": "game",
    "values": [["terminal", "game-over"], ["clearedRows", "4"], ["score", "1200"]]
  },
  "screenshot": {
    "provesScreenshot": true,
    "blockedStage": null,
    "unsupportedHostReason": null,
    "fallback": null,
    "path": "frame.png"
  }
}
```

## Rules

- **R-E1 (non-empty disclosure)**: `notAuthoritativeFor` MUST be non-empty for every record (FR-007/SC-003).
- **R-E2 (outcome match)**: `outcome` MUST equal the sample's authored `ExpectedOutcome`; a mismatch fails
  `BuildOutcomeTests` (FR-009/SC-001). `outcome` is included whether or not a screenshot was proven.
- **R-E3 (determinism)**: two runs with the same `seed` MUST produce byte-identical `run.json` + `state.txt`
  (FR-006/SC-002). `authoritativeFor` includes `"determinism"` and `"tree-equality"` always; `"outcome"`
  always (it's checked headlessly); `"non-blank-offscreen-png"` only when the screenshot is proven.
- **R-E4 (degrade-and-disclose)**: when GL/capture is unavailable, `provesScreenshot=false`,
  `blockedStage="capture"`, `unsupportedHostReason` states why, `fallback="deterministic-state-only"`,
  `path=null`, and no `frame.png` is left on disk — exit still `0` (FR-008/SC-004). The deterministic state
  + outcome remain authoritative.
- **R-E5 (no timing in goldens)**: `state.txt` excludes all `*Duration` metrics so the golden is
  wall-clock-independent (as G1).
- **R-E6 (bounded run)**: the replayed script MUST drive each game to its terminal state (`outcome` carries
  `["terminal", …]`) within the scripted steps — no unbounded loop (SC-007).
