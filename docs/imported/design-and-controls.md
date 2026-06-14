---
title: Design and controls
category: FS.GG
categoryindex: 6
index: 4
description: Boundary between design-system primitives, themes, semantic controls, and design-specific kits.
---

# Design and controls

Design and controls belong with the rendering product, not with governance. They
should still be separate layers inside the rendering project: controls own
semantics and behavior, while design-system and theme layers own visual
decisions.

## Ownership

The rendering repository owns:

- semantic controls such as `Button`, `TextBox`, `ComboBox`, `DataGrid`, and
  `Dialog`;
- design-system primitives such as tokens, theme records, density, typography,
  radii, shadows, color roles, motion, icons, and visual-state rules;
- concrete themes such as Ant Design, Fluent, or Material-inspired themes;
- optional design-specific kits where a design system defines real product
  patterns beyond styling.

The governance repository does not own design decisions. It may later provide
optional drift checks or report generators, but the product contract remains in
the rendering project.

## Layering model

Use this conceptual package/module split unless implementation pressure proves a
different shape:

```text
Rendering.Core
  Scene, layout, input, drawing primitives, host-independent behavior.

Controls
  Semantic controls and behavior contracts.

DesignSystem
  Token model, theme model, component token slots, density, typography,
  radii, color roles, visual-state rules.

Themes
  Concrete theme values and style mappings, for example AntDesign, Fluent,
  Material.

Kits or Patterns
  Optional design-specific compositions, for example AntDesign.Form or
  AntDesign.Table, when the design system has behavior or layout conventions
  that are more than a visual skin.
```

The exact package IDs can be decided during repository extraction or rebrand.
The architectural rule is more important than the initial package count: controls
must not fork just because a theme changes.

## Same controls, different themes

The default is one semantic control set with multiple themes:

```text
Controls.Button + Themes.AntDesign
Controls.Button + Themes.Fluent
Controls.Button + Themes.Material
```

`Button` should keep one behavior and accessibility contract. The theme changes
component tokens, colors, spacing, typography, radius, border, shadow, icons,
and visual-state styling. It should not create `AntButton`, `FluentButton`, and
`MaterialButton` as separate behavior surfaces by default.

This keeps tests, API docs, accessibility behavior, keyboard behavior, focus
rules, and generated examples from multiplying for every design language.

## When a design system gets its own kit

A design system gets its own kit or pattern module when it introduces a real
product pattern rather than a visual treatment.

Theme-level changes:

- color palettes;
- typography scale;
- component spacing;
- density;
- border radii;
- shadows;
- hover, pressed, disabled, focused, and validation visuals;
- default icons.

Pattern or kit-level changes:

- form layout and validation flow;
- table filtering, sorting, density, and empty-state conventions;
- layout primitives that imply child structure;
- result, statistic, description, or page-header components;
- opinionated loading, error, empty, and success states;
- data-entry workflows that combine controls into a reusable composition.

For example, Ant Design should usually be a theme for `Button`, `TextBox`, and
`Dialog`. It may also justify `AntDesign.Form`, `AntDesign.Table`,
`AntDesign.Result`, `AntDesign.Descriptions`, or `AntDesign.Statistic` if those
components encode Ant Design's layout and interaction conventions.

## Decision rule

Use the smallest layer that preserves the contract:

| Change type | Layer |
|---|---|
| Visual tokens, color, spacing, typography, radius, shadow, density, icon choice, visual states. | Theme |
| Shared style slots or token names needed across themes. | Design system |
| Input behavior, focus behavior, accessibility role, state machine, value model, command semantics. | Control |
| Opinionated composition, data workflow, validation layout, table behavior, expected child structure. | Kit or pattern |

If a design choice changes only how a control looks, it is a theme. If it
changes what the control is or how users interact with it, it is a control or
pattern decision.

## Planning implication

The rendering split should create these boundaries before adding large branded
design systems. First stabilize the semantic controls and token/theme model.
Then add concrete themes. Only after that should the project add design-specific
kits for patterns that cannot be represented as styling over the shared controls.
