# Contract: net-new generic controls in `FS.GG.UI.Controls`

Net-new controls are **generic and theme-agnostic** (no Ant naming, no theme branching). Each is added as: a `catalog.yml` entry → regenerated `Catalog.fs` GENERATED row → a curated `.fsi` → a `.fs` body, grouped into cohesive modules.

## Per-control requirements

- **R1 (catalog parity)**: the control appears in `catalog.yml` with `id`, `category`, `module`/`typedModule`, required/common attributes, the standard 8 `visualStates`, `accessibility` metadata, `events`, `supportStatus: supported` — same schema as existing rows. `Catalog.fs` regenerated.
- **R2 (`.fsi` first)**: a curated signature exists before the `.fs`; visibility lives in the `.fsi` (no access modifiers in `.fs`). Module shape follows existing presentational controls (e.g. `Badge`):

  ```fsharp
  module Tag =
      val create: attrs: Attr<'msg> list -> Control<'msg>
      // + control-specific attribute helpers (text/color/closable/onClose, etc.)
  ```

- **R3 (dual-theme render)**: renders coherently under `Themes.Default` (neutral) and `Themes.AntDesign` (Ant-styled). All appearance differences come from the resolver/tokens, not the control.
- **R4 (no fork)**: the control never reads theme identity to alter behaviour (parity test enforces).
- **R5 (state)**: presentational controls are pure render + attributes + events (parent owns state). Workflow-bearing controls (if any: Cascader/Transfer/Upload) expose `Model`/`Msg`/`Effect` like `DataGrid`/`Collections`; no ad-hoc internal mutable state.
- **R6 (five test families)**: passes Catalog/Semantic/Interaction/Accessibility/Rendering tests, same as every existing control.

## Candidate net-new control ids (finalized in P-B against the matrix)

`tag`, `avatar`, `alert`, `collapse`, `segmented`, `rate`, `timeline`, `steps`, `breadcrumb`, `pagination`, `card`, `descriptions`, `statistic`, `result`, `empty`, `drawer`, `popover`, `popconfirm`, `skeleton`, `affix`, `watermark`, `carousel`, `float-button`, `anchor`, `auto-complete`, `mentions`, `cascader`, `transfer`, `upload`, `calendar`, `qr-code`, `tour`.

> The exact net-new vs composition split is decided when the matrix is authored (P-B). Rule: add a primitive only when a composition of existing controls cannot express the interaction or visual-state cleanly. Borderline items (Card, Steps, Breadcrumb, Result, Descriptions) may resolve to `composition` instead.

## Surface-baseline expectation

`tests/surface-baselines/FS.GG.UI.Controls.txt` regenerated — the only new rows are the net-new control modules (and their `Model`/`Msg`/`Effect`/attribute helper types where applicable). No incidental surface leaks (a baseline diff review is part of the change).
