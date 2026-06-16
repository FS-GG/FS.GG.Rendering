// GENERATED — do not edit. Source: src/Themes.Default/design-tokens.tokens.json
// Regenerate via: dotnet fsi scripts/generate-design-tokens.fsx
//
// Feature 130 (Workstream F, F5): the PUBLIC signature for the Ant-derived token taxonomy —
// the deliberate, baseline-gated promotion F1 deferred. Generated in lock-step with the paired
// DesignTokensExt.fs; currency is enforced by this generator's --check over BOTH files.
namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

/// The Ant-derived design-token taxonomy: seed -> map -> alias -> component layers plus spacing,
/// density, type scale, and elevation. Generated from the DTCG source; values are byte-identical
/// to the flat primitives.
module DesignTokensExt =

    /// Seed layer: the primitive brand/semantic colors and base scalar units the map layer derives from.
    module Seed =
        val colorPrimary : Color
        val colorSuccess : Color
        val colorWarning : Color
        val colorError : Color
        val colorInfo : Color
        val colorTextBase : Color
        val colorBgBase : Color
        val fontSize : float
        val lineHeight : float
        val borderRadius : float
        val controlHeight : float
        val sizeUnit : float
        val sizeStep : float
        val motionUnit : float

    /// Map layer: semantic color roles per light/dark mode, derived from the seed.
    module Map =
        /// Light-mode values.
        module Light =
            val colorPrimaryHover : Color
            val colorPrimaryActive : Color
            val colorPrimaryBg : Color
            val colorErrorBg : Color
            val colorBorder : Color
            val colorFillSecondary : Color
            val colorBgContainer : Color
            val colorBgElevated : Color
            val colorBgLayout : Color
            val colorText : Color
            val colorTextSecondary : Color
            val colorTextDisabled : Color

        /// Dark-mode values.
        module Dark =
            val colorPrimaryHover : Color
            val colorPrimaryActive : Color
            val colorPrimaryBg : Color
            val colorErrorBg : Color
            val colorBorder : Color
            val colorFillSecondary : Color
            val colorBgContainer : Color
            val colorBgElevated : Color
            val colorBgLayout : Color
            val colorText : Color
            val colorTextSecondary : Color
            val colorTextDisabled : Color


    /// Alias layer: intent-named semantic aliases (text/surface/border/feedback) per light/dark mode.
    module Alias =
        /// Light-mode values.
        module Light =
            val textDefault : Color
            val textSecondary : Color
            val surfaceCanvas : Color
            val surfaceContainer : Color
            val surfaceElevated : Color
            val borderDefault : Color
            val itemHoverBg : Color
            val itemSelectedBg : Color
            val focusRing : Color
            val feedbackErrorText : Color
            val feedbackWarningText : Color

        /// Dark-mode values.
        module Dark =
            val textDefault : Color
            val textSecondary : Color
            val surfaceCanvas : Color
            val surfaceContainer : Color
            val surfaceElevated : Color
            val borderDefault : Color
            val itemHoverBg : Color
            val itemSelectedBg : Color
            val focusRing : Color
            val feedbackErrorText : Color
            val feedbackWarningText : Color


    /// Component layer: per-component color tokens (Button, Input, Table, Tabs, Menu, …).
    module Component =
        /// The Button token sub-group.
        module Button =
            val primaryBg : Color
            val primaryHoverBg : Color
            val defaultBorder : Color
            val dangerBg : Color

        /// The Input token sub-group.
        module Input =
            val activeBorder : Color
            val hoverBorder : Color
            val placeholderText : Color

        /// The Table token sub-group.
        module Table =
            val headerBg : Color
            val rowHoverBg : Color
            val borderColor : Color

        /// The Tabs token sub-group.
        module Tabs =
            val itemSelectedColor : Color
            val inkBar : Color
            val itemColor : Color

        /// The Menu token sub-group.
        module Menu =
            val itemSelectedBg : Color
            val itemSelectedColor : Color
            val itemHoverBg : Color


    /// Spacing scale (xs…xl), in layout units.
    module Space =
        val xs : float
        val sm : float
        val md : float
        val lg : float
        val xl : float

    /// Density multipliers (comfortable/middle/compact).
    module Density =
        val comfortable : float
        val middle : float
        val compact : float

    /// Type scale: font-size/line-height per typographic role.
    module Type =
        /// The Display token sub-group.
        module Display =
            val fontSize : float
            val lineHeight : float

        /// The Section token sub-group.
        module Section =
            val fontSize : float
            val lineHeight : float

        /// The Title token sub-group.
        module Title =
            val fontSize : float
            val lineHeight : float

        /// The Body token sub-group.
        module Body =
            val fontSize : float
            val lineHeight : float

        /// The Small token sub-group.
        module Small =
            val fontSize : float
            val lineHeight : float


    /// Elevation shadow tokens (none/low/medium/high).
    module Elevation =
        val none : string
        val low : string
        val medium : string
        val high : string
