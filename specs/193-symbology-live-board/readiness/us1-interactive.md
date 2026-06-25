# US1 interactive evidence (T013) — live moving board

**Goal (US1/SC-003/SC-004)**: every approved-roster unit renders as its fixed-grammar symbol, each animates
continuously and smoothly between fixed steps, none drifts off-board; degrade gracefully to a skip notice on
a headless host.

## Interactive launch (this checkout — live window host present)

```
$ dotnet run --project samples/SymbologyBoard -- interactive
Gtk-Message: Failed to load module "colorreload-gtk-module"
Gtk-Message: Failed to load module "window-decorations-gtk-module"
symbology-board: interactive session ended (status=ok).        # exit 0
```

`Viewer.runtimeCapability()` reported `PersistentWindow = true` on this host, so the board launched through
`ControlsElmish.runInteractiveApp` and the session returned `Ok` (`status=ok`, exit 0) — the live path, not
the headless skip. (The benign Gtk module warnings are the host environment, not the sample.)

## Headless-host behavior (SC-004) — by construction

When `Viewer.runtimeCapability().PersistentWindow` is false, `runInteractive` prints the canonical notice
and exits 0 without blocking/crashing (cli-contract.md):

```
symbology-board: interactive mode skipped — no live window/GL host.
```

This is the same `Viewer.runtimeCapability()` gate the accepted `samples/CanvasDemo` uses.

## On-board + smoothness invariants (covered by tests + structure)

Automated visual scrubbing of "all units animate smoothly" is **environment-limited** in this CI-style host
(the window opened and the session returned ok, but per-frame visual capture is not available here). The
substantive guarantees are nonetheless proven:

- **None drifts off-board** — `BoardTests` "on-board invariant" advances the fixed roster through 600 steps
  and asserts every unit centre stays within `[radius, extent-radius]` on both axes (FR-011/SC-003). 🟢
- **Non-blank board** — the non-empty-board and zero-area-symbol tests prove every unit (even degenerate)
  renders visible canonical bytes. 🟢
- **Smooth between steps** — `renderScene` interpolates each unit's Previous→Current centre by
  `Loop.alpha dt` and overlays `Symbology.animate`, the identical mechanism accepted in `samples/CanvasDemo`.

**Disclosure**: visual smoothness/per-unit-presence is asserted structurally + via the deterministic tests,
not via a captured frame sequence (environment-limited). The interactive launch returning `Ok` confirms the
window/host path is wired and does not crash.
