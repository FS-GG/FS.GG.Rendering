# Contract: Enterprise Template Pages (FR-005 / FR-006 / SC-002 / SC-009)

Six enterprise template pages, each a **composition of catalog controls only** (no bespoke control types),
realized from the committed `docs/product/ant-design/templates/*.md` "groundwork" recipes (which explicitly
name Workstream G3 as their consumer and list the controls + tokens each composes). Rendered under the Ant
theme; populated from `DemoState`.

## Composition rule (SC-002)

For every template page, **every node in its rendered `Control` tree maps to a known
`Catalog.supportedControls` id** (or a pure layout container that is itself a catalog control: stack, grid,
panel, border, …). `TemplateTests` walks each template's tree and fails if any node is not a catalog
control type. No new control types are introduced by this feature.

## The six templates

| Template | Shape (per recipe) | Catalog controls |
|---|---|---|
| **Workbench** (`tpl-workbench`) | top `toolbar` + primary `data-grid` working area + side `panel`s with `card`/`statistic` summaries, `Space.lg` gutters | toolbar, data-grid, panel, card, statistic |
| **List** (`tpl-list`) | filter `toolbar` + paginated collection + `pagination`; row `tag`s for status | toolbar, data-grid/list-view, pagination, tag |
| **Detail** (`tpl-detail`) | `descriptions` record view + related `card`/`panel`s + `tabs`; activity `timeline` | descriptions, card, panel, tabs, timeline |
| **Form** (`tpl-form`) | sectioned form fields + `validation-message`s + submit `button` → `result` on success | text-box, text-area, combo-box, switch, validation-message, button, result |
| **Result** (`tpl-result`) | `result` success/info state + follow-up `button`s + a `statistic` | result, button, statistic |
| **Exception** (`tpl-exception`) | `result`-based 403 / 404 / 500 state + recovery `button` + status `icon` | result, button, icon |

## Form-validation contract (FR-006 / SC-009)

The Form template owns a `FormState` (data-model §5a). Validation runs in the pure `update` on
`FormSubmitted`:

- **Invalid** (empty `Name`, malformed `Email`, or `Agree=false`): model → `Invalid errors`; the rendered
  tree shows `validation-message` controls and contains **no** `result`-success node; nothing is committed.
- **Valid**: model → `Submitted`; the rendered tree contains a `result` success node.

`TemplateTests` proves both transitions via pure `update` + a tree-shape assertion, and the form page's
seeded `Scripts.fs` entry drives an **invalid-then-valid** sequence so the headless evidence captures both
states deterministically.
