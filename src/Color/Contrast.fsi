namespace FS.GG.UI.Color

open FS.GG.UI.Scene

/// The element kind a color is used for; selects the WCAG threshold (FR-003).
type Role =
    | Text
    | GraphicOrUi
    | Decorative

/// WCAG conformance classification for a ratio + role (FR-003, FR-004a).
type Verdict =
    | Aaa
    | Aa
    | AaLarge
    | Fail
    | Exempt
    | Indeterminate

/// Ratio + role + verdict in one value (SC-004). For an `Indeterminate` input
/// `Ratio` is `nan` (System.Double.NaN — the documented not-applicable sentinel);
/// for an `Exempt` (Decorative) input `Ratio` carries the measured value but no
/// threshold is applied.
type ContrastResult =
    { Ratio: float
      Role: Role
      Verdict: Verdict }

/// WCAG 2.x relative-luminance + contrast measurement over Scene colors.
module Contrast =

    /// WCAG 2.x relative luminance of an opaque color (FR-001):
    /// 0.2126 R + 0.7152 G + 0.0722 B over sRGB-linearized channels.
    val relativeLuminance: color: Color -> float

    /// WCAG 2.x contrast ratio between two opaque colors (FR-002):
    /// (Llighter + 0.05) / (Ldarker + 0.05), in 1.0 .. 21.0.
    val ratio: a: Color -> b: Color -> float

    /// Composite a possibly-translucent color over an opaque background using
    /// deterministic source-over before measuring (FR-004).
    val compositeOver: background: Color -> foreground: Color -> Color

    /// Map a ratio + role to a verdict (FR-003). `Decorative` always returns
    /// `Exempt` regardless of ratio.
    val verdict: role: Role -> ratio: float -> Verdict

    /// Headline single call: ratio + role -> ContrastResult (SC-004). Composites
    /// `foreground` over `background` first if it carries alpha.
    val check: role: Role -> background: Color -> foreground: Color -> ContrastResult

    /// Solid-fill check from a Scene paint. Non-solid paints (gradient/shader/
    /// image fills) return Indeterminate, neither pass nor fail (FR-004a).
    val checkPaint: role: Role -> background: Color -> paint: Paint -> ContrastResult
