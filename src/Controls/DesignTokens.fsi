// DesignTokens.fsi — curated public surface of the generated token module (feature 069).
// Principle II: this hand-curated signature is the SOLE public-surface declaration; the
// paired DesignTokens.fs is GENERATED from src/Controls/design-tokens.tokens.json (the DTCG
// single source of truth) and carries no access modifiers. Regenerate via
// `./fake.sh build -t RefreshSurfaceBaselines`; currency is enforced by DesignTokenDrift.
//
// This is the additive public delta to FS.GG.UI.Controls (FR-002, FR-008, US3). The Theme
// type and the Theme module signatures are UNCHANGED; this is the only new public surface.

namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

/// Typed, compiler-checked design-token values generated from
/// `src/Controls/design-tokens.tokens.json` (the DTCG single source of truth).
/// Token VALUES are generated; this curated signature is the sole public-surface declaration.
/// Token references are greppable and stay in lock-step with the DTCG source via DesignTokenDrift.
module DesignTokens =

    /// Light-theme primitives (feed Theme.light; value-identical to the pre-feature literals).
    module Light =
        /// Light-theme primary foreground (text/icon) colour.
        val foreground : Color
        /// Light-theme surface/background colour.
        val background : Color
        /// Light-theme accent colour for primary/active emphasis.
        val accent : Color
        /// Light-theme danger/destructive colour for errors and destructive actions.
        val danger : Color
        /// Light-theme success colour for the `StyleVariant.Success` style variant (feature 093).
        val success : Color
        /// Light-theme warning/caution colour for the `StyleVariant.Warning` style variant (feature 093).
        val warning : Color
        /// Light-theme muted colour for secondary text and de-emphasised chrome.
        val muted : Color
        /// Optional light-theme font family; <c>None</c> falls back to the host default.
        val fontFamily : string option
        /// Light-theme base font size in device-independent units.
        val fontSize : float
        /// Light-theme density multiplier scaling spacing and control sizing.
        val density : float
        /// Light-theme default corner radius for rounded control surfaces.
        val cornerRadius : float
        /// Minimum foreground/background contrast ratio the light theme must satisfy.
        val contrastRequiredRatio : float

    /// Dark-theme primitives (feed Theme.dark; value-identical to the pre-feature literals).
    module Dark =
        /// Dark-theme primary foreground (text/icon) colour.
        val foreground : Color
        /// Dark-theme surface/background colour.
        val background : Color
        /// Dark-theme accent colour for primary/active emphasis.
        val accent : Color
        /// Dark-theme danger/destructive colour (aliases the light-theme danger token in the DTCG source).
        val danger : Color
        /// Dark-theme success colour for the `StyleVariant.Success` style variant (feature 093).
        val success : Color
        /// Dark-theme warning/caution colour for the `StyleVariant.Warning` style variant (feature 093).
        val warning : Color
        /// Dark-theme muted colour for secondary text and de-emphasised chrome.
        val muted : Color
        /// Optional dark-theme font family; <c>None</c> falls back to the host default.
        val fontFamily : string option
        /// Dark-theme base font size in device-independent units.
        val fontSize : float
        /// Dark-theme density multiplier scaling spacing and control sizing.
        val density : float
        /// Dark-theme default corner radius for rounded control surfaces.
        val cornerRadius : float
        /// Minimum foreground/background contrast ratio the dark theme must satisfy.
        val contrastRequiredRatio : float
