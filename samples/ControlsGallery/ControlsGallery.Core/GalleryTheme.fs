/// Theme & accent resolution for the gallery: built-in Light/Dark over the shipped
/// slate neutral base, with consumer-defined Indigo/Teal accent literals (research R5).
/// The gallery is a *consumer*, so defining its own accent constants is legitimate —
/// `Theming.resolve` is the public accent seam and takes a `Color`.
module ControlsGallery.Core.GalleryTheme

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Theming

/// Indigo primary accent (#6366F1).
let indigo: Color = Colors.rgb 99uy 102uy 241uy

/// Teal secondary accent (#14B8A6).
let teal: Color = Colors.rgb 20uy 184uy 166uy

/// The accent selector set: stable id -> color. "Indigo & Teal on Slate".
let accents: (string * Color) list = [ "indigo", indigo; "teal", teal ]

/// Resolve a mode + accent into a renderable Theme over the slate neutral base.
let resolve (mode: ThemeMode) (accent: Color): Theme =
    Theming.toTheme (Theming.resolve mode accent)

/// A neutral default theme (Light + indigo) for content that needs a `Theme` at build
/// time (e.g. rich-text default style). Rendering always re-themes per the live model.
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

/// The next accent in the cycle (for a single-button accent toggle).
let nextAccent (accent: Color): Color =
    if accent = indigo then teal else indigo
