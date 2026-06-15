# Interactive-feature readiness checklist

When a feature ships an **interactive** surface (a host the user drives with pointer/keyboard via
`Controls.Elmish.runInteractiveApp`), an interactive `EvidenceAudit` expects the
**window-visibility-class** readiness set under `specs/<feature>/readiness/`. Each file uses simple
`key=value` tokens (one per line) — the audit reads tokens, not prose — so author them against this
contract rather than reverse-engineering a gate failure.

## Required readiness files + tokens

| File | Required tokens |
|------|-----------------|
| `interactive-visible-window.md` | `status=`, `mode=`, `window-visible=`, `accessible-window=`, `first-frame-presented=`, `self-closed-for-evidence=` |
| `close-reason-separation.md` | `close-reason=`, `user-close-observed=`, `evidence-close-observed=` |
| `window-state-diagnostics.md` | one `diagnostic-class=… status=…` line per class, plus `native-handle=`, `visible=`, `focusable=`, `renderable-surface=`, `input-devices=` |
| `window-options.md` | one `option=… status=… observed=…` line per window option (resize / maximize / startup-state / startup-position / backend) |
| `real-image-evidence.md` | `evidence-kind=`, `status=`, `artifact-decodable=`, `proves-scene-rendering=`, `proves-desktop-visibility=` |
| `generated-validation.md` | `package-resolution=resolved`, `package-mismatch=false`, `exact-package-match=`, `generated-tests-exist=`, `generated-tests-ran=`, `authoritative=`, `failure-class=` |
| `evidence-audit.md` | a `verdict=` token (e.g. `verdict=PASS`) |

A render-only / responds-proof feature (no NEW live window) records each as `not-applicable` with an
honest reason — the values must be **honest**, not merely present. A feature that DOES open a live
window records the observed truth (`window-visible=true`, `first-frame-presented=true`, …).

## Host-seam authority

The interactive seam (`runInteractiveApp`, `InteractiveAppHost`, `PointerInteraction`, `Perf.runScript`)
is **present in the `FS.GG.UI.Controls.Elmish` package, not in `docs/api-surface/`** — its authority
is the `fs-gg-controls-host` skill + `ControlsElmish.fsi` (see `scaffold-map.md`). Focus visibility is
the public `Focus.markFocused model.Focused (view …)` call inside `view`.
