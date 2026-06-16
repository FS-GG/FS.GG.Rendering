# Contract: `designSystem` Template Parameter

The external **scaffolding contract** F3 adds to the project template (`.template.config/template.json`).
This is the command-schema surface a maintainer interacts with via `dotnet new fs-gg-ui`.

## Parameter declaration

Added to `symbols` (modeled on the existing `profile` choice and `feedback` no-diff defaults):

```jsonc
"designSystem": {
  "type": "parameter",
  "datatype": "choice",
  "defaultValue": "wcag",
  "description": "Governing color policy for the generated product. 'wcag' (default) is byte-identical to today; 'ant' imprints the Ant design language and Ant contrast policy. Unknown values are rejected.",
  "choices": [
    { "choice": "wcag", "description": "WCAG 2.x contrast governance (default; no diff vs today's template)." },
    { "choice": "ant",  "description": "Ant Design contrast expectations over the Ant-derived tokens." }
  ]
}
```

CLI surface: `dotnet new fs-gg-ui --designSystem wcag|ant` (short alias `-de`). dotnet new derives the
option name from the symbol name **verbatim**, so the camelCase symbol `designSystem` surfaces as
`--designSystem`, **not** a kebab `--design-system` (the single-word `profile` symbol hid this — there is
no kebab transform). The symbol stays camelCase because a hyphenated symbol name (`design-system`) would
be parsed as subtraction inside the `(designSystem == "ant")` conditional-source expression and break it.

## Conditional sources (the `ant`-only overlay — `feedback` precedent)

Added to `sources`, firing **only** for `ant`; **no** entry fires for `wcag`:

```jsonc
{ "condition": "(designSystem == \"ant\")", "source": "template/design-system/ant/", "target": "./" }
```

`template/base/` is **not** edited.

## Behavioral guarantees

| ID | Guarantee | Maps to |
|---|---|---|
| TP-1 | Default (no `--designSystem`) ⇒ `wcag`. | FR-002 |
| TP-2 | `wcag` (default or explicit) ⇒ **zero** new/changed content; scaffold byte-identical to today and to the no-value scaffold. | FR-003/SC-001 |
| TP-3 | `ant` ⇒ the overlay is copied: a `design-system.json` recording `policy:"ant"` + the Ant policy report (the F1/F2 imprint as data). | FR-004/FR-005/SC-002 |
| TP-4 | A genuinely unrecognized value (e.g. `material`, `fluent`, `antd`) ⇒ `dotnet new` **rejects** it and surfaces the accepted set; **no** product is generated with a substituted policy. A recognized value differing only in **case** is *not* an unknown value — see TP-5. | FR-007/SC-005 |
| TP-5 | Casing/format matching follows the engine's existing `choice` rules (the **same** behavior as `profile`); no new matching scheme. A case-variant of a real value (e.g. `Ant`) resolves the way `profile`'s engine resolves its own case-variants — it is **not** treated as an unknown value by TP-4. | edge case "case/formatting"; assumption "template-mechanism reuse" |
| TP-6 | Orthogonal to `profile`: any `profile` × any `designSystem` scaffolds and behaves consistently. | edge case "interaction with profile" |
| TP-7 | The choice set is the single enumerable source the validation reads for coverage; adding a value = one `choices` entry + one overlay dir, no reshaping. | FR-009; edge case "future policies" |
| TP-8 | No package reference, React/DOM/web/icon-font dependency is added by the overlay. | FR-012 |

## Non-goals (deferred)

- No runtime consumption of the policy by the generated product (F4 wires the style resolver).
- No public package API for policy selection (F5 promotes the surface).
- The generated product does **not** call `ColorPolicy`; it records a choice the framework validates.
