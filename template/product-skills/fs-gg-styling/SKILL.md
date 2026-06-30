---
name: fs-gg-styling
description: Generated product guidance for theming an FS.GG.UI product and styling controls — pick a theme, set a control's style variant and class, and consume the resolved style.
---

# Styling & Theming

## Scope

Use this skill for generated product screens that need to look themed and to vary
by intent and interaction state. It covers the **consumer slice** of styling:

- pick and apply a theme,
- set a control's style variant and style class,
- consume the style the framework resolves for the control.

It does **not** cover building the styling pipeline — see [Boundary](#boundary).

## Public Contract

Theming and styling are reached through three public surfaces:

- `FS.GG.UI.Themes.Default.Theme` — built-in `Theme` values and adjusters.
- `FS.GG.UI.DesignSystem` — `StyleVariant`, `StyleClass`, `VisualState`, `ResolvedStyle`.
- `Attr` styling builders on `FS.GG.UI.Controls` — `theme`, `styleClasses`, `style`,
  `visualState`, `validation`.

## Pick & apply a theme

The host carries the active theme; every control under it inherits it.

```fsharp
open FS.GG.UI.Themes.Default

// Built-in palettes:
Theme.light
Theme.dark

// Adjust a built-in theme (pure; returns a new Theme):
Theme.light |> Theme.withAccent brandColor      // swap the accent role
Theme.light |> Theme.withDensity 0.85           // compact spacing/sizing
```

Set it once on the host so the whole tree paints against it:

```fsharp
let host =
    { // ...Init/Update/View...
      Theme = Theme.dark }
```

Override the theme for one subtree with the `theme` attribute — the control and its
descendants paint against the override instead of the inherited theme:

```fsharp
Stack.create [
    Attr.theme (Theme.light |> Theme.withAccent brandColor)
    Stack.children [ (* this subtree is themed independently *) ]
]
```

### Live theming (mode + accent)

To recompute a theme at runtime from a mode and an accent, use `Theming`:

```fsharp
open FS.GG.UI.Themes.Default.Theming

let theme =
    Theming.resolve ThemeMode.Dark brandAccent   // -> RolePalette
    |> Theming.toTheme                            // -> Theme  (pass to the host / `Attr.theme`)
```

Keep a static `host.Theme` as the fragment-reuse key and pass the recomputed theme to
the render path when the captured palette must be exact.

## Set a control's style variant & class

A control carries an ordered `StyleClass list`. Each entry is either a typed semantic
`Variant` or a free-form `Custom` class. List order is attach order (earlier first).

```fsharp
open FS.GG.UI.DesignSystem

// Semantic variant — the compiler-checked common path.
// StyleVariant is [<RequireQualifiedAccess>], so qualify the case: StyleVariant.Primary
Button.create [
    Button.text "Save"
    Attr.styleClasses [ StyleClass.Variant StyleVariant.Primary ]
]

// Variant plus a product-owned class (attach order: variant, then product class):
Button.create [
    Button.text "Delete"
    Attr.styleClasses [ StyleClass.Variant StyleVariant.Danger; StyleClass.Custom "compact" ]
]

// A single free-form class by name (counterpart to the typed builder):
Button.create [
    Button.text "Cancel"
    Attr.style "toolbar-compact"
]
```

Built-in variants: `Primary`, `Danger`, `Ghost`, `Neutral`, `Success`, `Warning`.

Reflect interaction and validity so the look follows state:

```fsharp
TextBox.create [
    TextBox.value model.Email
    Attr.visualState (if model.Editing then VisualState.Focused else VisualState.Normal)
    Attr.validation (if model.EmailValid then ValidationState.Valid else ValidationState.Invalid "Enter a valid email")
]
```

`visualState` absent ≡ `Normal`; `styleClasses` absent ≡ `[]` — both the
behaviour-preserving base case.

## Consume the resolved style

The framework folds the active **theme** + the control's **style classes** + its
**visual state** into a `ResolvedStyle` — the concrete paint and typography
(`Foreground`, `Fill`, `Stroke`/`StrokeWidth`, `FontFamily`/`FontSize`/`FontWeight`)
the control paints from. You do not call the resolver: you **declare** theme, classes,
and state on the control, and the produced product renders the resolved result.

So the product author's loop is: choose a theme, attach the variant/class/state that
expresses intent, and the control shows up themed and state-aware. To change how a
control looks, change the theme it inherits or the classes/state you attach to it — not
any pipeline internals.

## Boundary

This skill stays in the consumer slice. It does **not** document:

- the resolver pipeline — how `theme + classes + state` is folded into a `ResolvedStyle`
  (precedence, last-writer-wins, state/variant layering). Consume the result; do not
  reimplement the fold.
- the design-token source — authoring or generating the tokens behind a `Theme`.
- surface baselines — the framework-governed `.fsi` / baseline authoring.

When you need behaviour beyond declaring theme + variant/class + state, you have reached
the framework's styling pipeline, which is owned upstream — not product-author surface.

## Build & Test Commands

Run `./fake.sh build -t Dev` and `./fake.sh build -t Verify` in the generated product;
`./fake.sh build -t Test` for product-owned styling examples.

## Generated Product

Keep examples small and product-owned. Style the controls your product actually ships;
do not copy framework galleries, framework themes, or framework styling internals.

## Related

- [[fs-gg-ui-widgets]] — compose the controls you style here.
- [[fs-gg-scene]] — the primitive layer controls paint into.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
