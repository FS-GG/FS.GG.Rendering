namespace FS.Skia.UI.Color

open FS.Skia.UI.Scene

/// Radix-derived, role-labelled accessible ramps (FR-005, FR-006). Reusable
/// catalog data only — NOT a second source of truth for shipped themes. The WCAG
/// gate, not this source palette, certifies conformance of any chosen value.
module Palettes =

    type StepRole =
        | AppBackground
        | SubtleBackground
        | ComponentBackground
        | Border
        | FocusRing
        | Solid
        | Text

    type RampVariant =
        | Light
        | Dark

    type PaletteStep =
        { Index: int
          Role: StepRole
          Color: Color }

    type PaletteRamp =
        { Family: string
          Variant: RampVariant
          Steps: PaletteStep list }

    /// Every available ramp (matched light + dark per family).
    val all: PaletteRamp list

    /// Look up a ramp by family + variant.
    val ramp: family: string -> variant: RampVariant -> PaletteRamp option

    /// The family names offered.
    val families: string list
