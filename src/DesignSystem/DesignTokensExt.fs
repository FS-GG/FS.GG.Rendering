// GENERATED — do not edit. Source: src/Themes.Default/design-tokens.tokens.json
// Regenerate via: dotnet fsi scripts/generate-design-tokens.fsx
//
// Feature 126 (Workstream F, F1): the Ant-derived token taxonomy (seed/map/alias/component +
// spacing/density/type/elevation). INTERNAL and additive — no .fsi, no public-surface delta;
// nothing reads these yet (the F4 resolver / D2 themes consume them later).
namespace FS.GG.UI.DesignSystem

open FS.GG.UI.Scene

module internal DesignTokensExt =

    module Seed =
        let colorPrimary : Color = Colors.rgba 22uy 119uy 255uy 255uy
        let colorSuccess : Color = Colors.rgba 21uy 128uy 61uy 255uy
        let colorWarning : Color = Colors.rgba 180uy 83uy 9uy 255uy
        let colorError : Color = Colors.rgba 185uy 28uy 28uy 255uy
        let colorInfo : Color = Colors.rgba 22uy 119uy 255uy 255uy
        let colorTextBase : Color = Colors.rgba 31uy 41uy 55uy 255uy
        let colorBgBase : Color = Colors.rgba 248uy 250uy 252uy 255uy
        let fontSize : float = 14.0
        let lineHeight : float = 1.5
        let borderRadius : float = 4.0
        let controlHeight : float = 32.0
        let sizeUnit : float = 4.0
        let sizeStep : float = 4.0
        let motionUnit : float = 0.1

    module Map =
        module Light =
            let colorPrimaryHover : Color = Colors.rgba 64uy 150uy 255uy 255uy
            let colorPrimaryActive : Color = Colors.rgba 9uy 88uy 217uy 255uy
            let colorPrimaryBg : Color = Colors.rgba 230uy 244uy 255uy 255uy
            let colorErrorBg : Color = Colors.rgba 255uy 242uy 240uy 255uy
            let colorBorder : Color = Colors.rgba 217uy 217uy 217uy 255uy
            let colorFillSecondary : Color = Colors.rgba 245uy 245uy 245uy 255uy
            let colorBgContainer : Color = Colors.rgba 255uy 255uy 255uy 255uy
            let colorBgElevated : Color = Colors.rgba 255uy 255uy 255uy 255uy
            let colorBgLayout : Color = Colors.rgba 245uy 245uy 245uy 255uy
            let colorText : Color = Colors.rgba 31uy 41uy 55uy 255uy
            let colorTextSecondary : Color = Colors.rgba 100uy 116uy 139uy 255uy
            let colorTextDisabled : Color = Colors.rgba 191uy 191uy 191uy 255uy

        module Dark =
            let colorPrimaryHover : Color = Colors.rgba 60uy 137uy 232uy 255uy
            let colorPrimaryActive : Color = Colors.rgba 22uy 104uy 220uy 255uy
            let colorPrimaryBg : Color = Colors.rgba 17uy 26uy 44uy 255uy
            let colorErrorBg : Color = Colors.rgba 44uy 22uy 24uy 255uy
            let colorBorder : Color = Colors.rgba 66uy 66uy 66uy 255uy
            let colorFillSecondary : Color = Colors.rgba 31uy 31uy 31uy 255uy
            let colorBgContainer : Color = Colors.rgba 31uy 31uy 31uy 255uy
            let colorBgElevated : Color = Colors.rgba 42uy 42uy 42uy 255uy
            let colorBgLayout : Color = Colors.rgba 0uy 0uy 0uy 255uy
            let colorText : Color = Colors.rgba 241uy 245uy 249uy 255uy
            let colorTextSecondary : Color = Colors.rgba 148uy 163uy 184uy 255uy
            let colorTextDisabled : Color = Colors.rgba 90uy 90uy 90uy 255uy


    module Alias =
        module Light =
            let textDefault : Color = Colors.rgba 31uy 41uy 55uy 255uy
            let textSecondary : Color = Colors.rgba 100uy 116uy 139uy 255uy
            let surfaceCanvas : Color = Colors.rgba 245uy 245uy 245uy 255uy
            let surfaceContainer : Color = Colors.rgba 255uy 255uy 255uy 255uy
            let surfaceElevated : Color = Colors.rgba 255uy 255uy 255uy 255uy
            let borderDefault : Color = Colors.rgba 217uy 217uy 217uy 255uy
            let itemHoverBg : Color = Colors.rgba 245uy 245uy 245uy 255uy
            let itemSelectedBg : Color = Colors.rgba 230uy 244uy 255uy 255uy
            let focusRing : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let feedbackErrorText : Color = Colors.rgba 185uy 28uy 28uy 255uy
            let feedbackWarningText : Color = Colors.rgba 180uy 83uy 9uy 255uy

        module Dark =
            let textDefault : Color = Colors.rgba 241uy 245uy 249uy 255uy
            let textSecondary : Color = Colors.rgba 148uy 163uy 184uy 255uy
            let surfaceCanvas : Color = Colors.rgba 0uy 0uy 0uy 255uy
            let surfaceContainer : Color = Colors.rgba 31uy 31uy 31uy 255uy
            let surfaceElevated : Color = Colors.rgba 42uy 42uy 42uy 255uy
            let borderDefault : Color = Colors.rgba 66uy 66uy 66uy 255uy
            let itemHoverBg : Color = Colors.rgba 31uy 31uy 31uy 255uy
            let itemSelectedBg : Color = Colors.rgba 17uy 26uy 44uy 255uy
            let focusRing : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let feedbackErrorText : Color = Colors.rgba 185uy 28uy 28uy 255uy
            let feedbackWarningText : Color = Colors.rgba 180uy 83uy 9uy 255uy


    module Component =
        module Button =
            let primaryBg : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let primaryHoverBg : Color = Colors.rgba 64uy 150uy 255uy 255uy
            let defaultBorder : Color = Colors.rgba 217uy 217uy 217uy 255uy
            let dangerBg : Color = Colors.rgba 185uy 28uy 28uy 255uy

        module Input =
            let activeBorder : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let hoverBorder : Color = Colors.rgba 64uy 150uy 255uy 255uy
            let placeholderText : Color = Colors.rgba 191uy 191uy 191uy 255uy

        module Table =
            let headerBg : Color = Colors.rgba 250uy 250uy 250uy 255uy
            let rowHoverBg : Color = Colors.rgba 245uy 245uy 245uy 255uy
            let borderColor : Color = Colors.rgba 240uy 240uy 240uy 255uy

        module Tabs =
            let itemSelectedColor : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let inkBar : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let itemColor : Color = Colors.rgba 31uy 41uy 55uy 255uy

        module Menu =
            let itemSelectedBg : Color = Colors.rgba 230uy 244uy 255uy 255uy
            let itemSelectedColor : Color = Colors.rgba 22uy 119uy 255uy 255uy
            let itemHoverBg : Color = Colors.rgba 245uy 245uy 245uy 255uy


    module Space =
        let xs : float = 4.0
        let sm : float = 8.0
        let md : float = 16.0
        let lg : float = 24.0
        let xl : float = 32.0

    module Density =
        let comfortable : float = 1.0
        let middle : float = 0.875
        let compact : float = 0.75

    module Type =
        module Display =
            let fontSize : float = 30.0
            let lineHeight : float = 1.3

        module Section =
            let fontSize : float = 20.0
            let lineHeight : float = 1.4

        module Title =
            let fontSize : float = 16.0
            let lineHeight : float = 1.5

        module Body =
            let fontSize : float = 14.0
            let lineHeight : float = 1.5

        module Small =
            let fontSize : float = 12.0
            let lineHeight : float = 1.5


    module Elevation =
        let none : string = "none"
        let low : string = "0 1 2 #0000000f"
        let medium : string = "0 4 12 #00000014"
        let high : string = "0 8 24 #0000001f"
