# Contract deltas: `.fsi` / diagnostics surface

**Feature**: 232 · Constitution II — `.fsi` is the visibility & contract source. This enumerates the
expected surface changes BEFORE implementation so the public-surface / ApiCompat gate outcome is
intentional. **No public *signature* removal is expected**; changes are doc text + one internal helper.

## Public signatures — UNCHANGED (doc text only)

- **`Focus.fsi`** — `order : Control<'msg> -> TabOrder` unchanged. Update the doc comment on
  `FocusStop.Control` / `order` to state the id is `Key ?? structural-path` (feature-098 unification),
  removing the `Key ?? Kind` wording. `markFocused` / `traverse` signatures unchanged.
- **`ControlRuntime.fsi`** — the runtime-bridge functions are **internal** (not in `.fsi`); no public
  signature change. If any `.fsi` doc references `Key ?? Kind` for the visual-state/scroll bridge,
  update it to `Key ?? path`.
- **`Diagnostics.fsi`** — the unkeyed same-kind collapse diagnostic keeps its public shape; update the
  doc comment (`Diagnostics.fsi:79`) from `(Key ?? Kind)` to the unified `Key ?? path` scheme and the
  remediation (`Control.withKey`).
- **Widget lowering** (`WidgetLowering` / `Pickers` / `Buttons` public surface) — `create` signatures
  unchanged; behavior change is internal (trigger keyed, real stops). No `.fsi` signature delta.

## Internal additions (NOT in `.fsi`)

- **`RetainedRender` (internal)** — add a path-aware resolver, e.g.
  `retainedCanonicalId : RetainedId -> RetainedRender<'msg> -> ControlId option`
  (or `tryFindNodeWithPath`) returning the node's full-tree `Key ?? path`. `internal`, reached via
  `InternalsVisibleTo` by `Controls.Elmish` and tests. Mirrors `authoredControlIds`' path walk. Because
  it is internal it does **not** appear in `RetainedRender.fsi`.

## Behavioral contract (tested, not signature)

- `Focus.order` stop ids equal `Control.eventBindingsOf` / `boundIdsOf` ids for the same node.
- Runtime visual-state / scroll bridges key by `Key ?? path`; at-rest bridged tree is byte-identical.
- `routeFocusedKey` dispatches an unkeyed focused control's activation bindings.
- DatePicker / SplitButton declared `triggerId` is carried by a real lowered control; no
  `MissingOverlayAnchor`; `focusScope` stops reference real ids.

## Gate expectation

- Public-surface snapshot / ApiCompat: **no unaccounted diff** (doc-comment-only changes; internal
  helper excluded). If the snapshot tracks doc text, update the accepted snapshot in the same feature
  with a note pointing here.
