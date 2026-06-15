# Contract: Page Evidence Record

The deterministic per-page artifact produced by `ControlsGallery evidence --seed N`.
Schema mirrors the existing harness (`tests/Rendering.Harness/Evidence.fs`),
re-implemented in the consumer (research R3) so the gallery stays package-only.

## Files (per page)

Written under `artifacts/controls-gallery/<seed>/<page-id>/`:

| File | Purpose |
|------|---------|
| `run.json` | Machine-readable proof record (fields below). |
| `summary.md` | Human-readable disclosure: proof level, authoritative-for, **not** authoritative-for, theme/accent, backend. |
| `state.txt` | The golden `FrameMetrics` (count/bool fields only). |
| `frame.png` | Screenshot of the required surfaces — **present** when GL available; **absent with a stated reason** otherwise. |

## `run.json` fields

```jsonc
{
  "pageId": "display-typography",
  "seed": 1234,
  "proofLevel": "deterministic",
  "authoritativeFor": ["determinism", "tree-equality", "non-blank-offscreen-png"],
  "notAuthoritativeFor": ["renderer-vs-desktop-pixels", "live-host", "timing"],
  "screenshot": {
    "provesScreenshot": true,
    "blockedStage": null,
    "unsupportedHostReason": null,
    "fallback": null,
    "path": "frame.png"
  }
}
```

## Invariants

1. **Disclosure (FR-010/SC-004)**: `notAuthoritativeFor` is **non-empty** for every
   record. No record claims more than it proves.
2. **Determinism (FR-009/SC-002)**: for a fixed `seed`, two consecutive runs over all
   pages produce **byte-identical** `run.json` + `state.txt` (and `frame.png` where GL
   is present). Achieved by: golden count/bool metrics only (no `*Duration`), seeded
   input, injected time deltas, no randomness.
3. **Degrade-and-disclose (FR-011/SC-004)**: on a host without display/GL, the state
   portion is still produced (it needs no GL), `screenshot.provesScreenshot=false`
   with a populated `unsupportedHostReason`/`fallback`, `frame.png` is omitted, and the
   process exits 0 — never a hang, never a fabricated pass.
4. **Acceptance mapping (SC-007)**: each page's seeded run satisfies the acceptance
   criteria carried by its source showcase spec (adopted per FR-015); the record's
   `authoritativeFor` reflects what was actually checked.
