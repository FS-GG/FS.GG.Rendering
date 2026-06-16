# Contract — token taxonomy (internal surface + DTCG source schema)

F1 adds **no public** contract; this documents the **internal** token surface the future resolver/themes
consume and the DTCG source schema the generator reads. Public promotion of any of this is F5.

## DTCG source schema (`src/Themes.Default/design-tokens.tokens.json`)

The existing top-level `light` / `dark` primitive objects are **unchanged**. New top-level groups are
added using DTCG `$type`/`$value` leaves:

```jsonc
{
  "light": { /* unchanged primitives */ },
  "dark":  { /* unchanged primitives */ },

  "seed":  { "colorPrimary": { "$type": "color", "$value": "#1677ffff" }, /* … */ },
  "map":   { "light": { "colorPrimaryHover": { "$type": "color", "$value": "#4096ffff" }, /* … */ },
             "dark":  { /* same keys */ } },
  "alias": { "light": { "text.default": { "$type": "color", "$value": "#1f2937ff" }, /* … */ },
             "dark":  { /* same keys */ } },
  "component": { "button": { "primaryBg": { "$type": "color", "$value": "#1677ffff" }, /* … */ },
                 "input": { /* … */ }, "table": {}, "tabs": {}, "menu": {} },

  "space":     { "xs": { "$type": "dimension", "$value": 4.0 }, /* … xl */ },
  "density":   { "comfortable": { "$type": "number", "$value": 1.0 }, "middle": {}, "compact": {} },
  "type":      { "body": { "fontSize": { "$type": "dimension", "$value": 14.0 },
                           "lineHeight": { "$type": "number", "$value": 1.5 } }, /* … */ },
  "elevation": { "none": { "$type": "shadow", "$value": "none" }, /* low/medium/high */ }
}
```

**Schema rules**
- Colors are 8-digit `#rrggbbaa` strings (matching existing primitives).
- `dimension`/`number` are JSON numbers (floats).
- `shadow` values are opaque strings in F1 (rendered later).
- Every `map.*`/`alias.*` key present under `light` MUST be present under `dark` (mode parity, V2).
- The generator MUST fail loudly on a malformed leaf, a mode-parity gap, or an unknown `$type`.

## Internal generated module (`module internal DesignTokensExt`)

- Declared `module internal` in `FS.GG.UI.DesignSystem`; **no `.fsi`**; reached via
  `InternalsVisibleTo` (F1 grants `Controls.Tests`; the F4 resolver is added when it lands).
- Shape mirrors `data-model.md` "Generated-module surface". Color leaves → `Color`
  (`Colors.rgba r g b a`); dimension/number leaves → `float`; shadow → `string`.
- Name mapping: a dotted DTCG key becomes a camelCase F# identifier — `text.default` → `textDefault`,
  `item.hoverBg` → `itemHoverBg`. The mapping is total and deterministic.
- Ordering is deterministic (source order or sorted keys, fixed by the generator) so regeneration is
  byte-stable.

## Consumption contract (informational — consumers are out of scope)

- The future F4 `resolve` reads `Seed`/`Map`/`Alias`/`Component` to produce a `ControlStyle`; D2 themes
  read them to populate concrete `Theme`-layer values. **Neither exists in F1.**
- No control, renderer, `Theme` value, or sample reads `DesignTokensExt` in F1 (V5/FR-008).

## Non-goals (explicit)

- No public `.fsi` for these tokens (F5).
- No change to the public `DesignTokens` module or `Theme` record.
- No algorithmic derivation of `map`/`alias` from `seed` (explicit values only — R6).
