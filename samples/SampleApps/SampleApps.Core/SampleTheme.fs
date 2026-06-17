/// Theme & accent resolution for the samples: built-in Light/Dark over the shipped
/// neutral base, with consumer-defined Indigo/Teal accent literals (research R9, mirrors
/// G1's `GalleryTheme`). The samples are *consumers*, so defining their own accent
/// constants is legitimate — `Theming.resolve` is the public accent seam and takes a
/// `Color`. No dependence on the Ant/Fluent/Material themes or any kit (FR-015).
module SampleApps.Core.SampleTheme

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default.Theming
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

/// Indigo primary accent (#6366F1).
let indigo: Color = Colors.rgb 99uy 102uy 241uy

/// Teal secondary accent (#14B8A6).
let teal: Color = Colors.rgb 20uy 184uy 166uy

/// The accent selector set: stable id -> color.
let accents: (string * Color) list = [ "indigo", indigo; "teal", teal ]

/// Resolve a mode + accent into a renderable Theme over the neutral base.
let resolve (mode: ThemeMode) (accent: Color): Theme =
    Theming.toTheme (Theming.resolve mode accent)

/// A neutral default theme (Light + indigo) for content that needs a `Theme` at build
/// time. Rendering always re-themes per the live model.
let defaultTheme: Theme = resolve Light indigo

/// Stable accent id for a color (status display / round-trip); defaults to indigo.
let accentId (accent: Color): string =
    accents
    |> List.tryPick (fun (id, c) -> if c = accent then Some id else None)
    |> Option.defaultValue "indigo"

/// Look up an accent color by id (the selector); defaults to indigo.
let accentById (id: string): Color =
    accents
    |> List.tryPick (fun (k, c) -> if k = id then Some c else None)
    |> Option.defaultValue indigo
