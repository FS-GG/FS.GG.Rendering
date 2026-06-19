module SecondAntShowcase.Core.VisualConfig

open FS.GG.UI.Scene
open SecondAntShowcase.Core.Model

type AcceptedSizeRole =
    | Preferred
    | Minimum
    | Custom

val preferredSize: Size
val minimumSize: Size
val preferredSizeText: string
val minimumSizeText: string
val supportedThemeIds: string list
val minimumRepresentativePageIds: string list
val visualReadinessStatusAccepted: string
val visualReadinessStatusBlocked: string
val visualReadinessStatusEnvironmentLimited: string
val parseSize: text: string -> Result<Size, string>
val sizeText: size: Size -> string
val classifySize: size: Size -> AcceptedSizeRole
val roleName: role: AcceptedSizeRole -> string
val resolveThemeAlias: text: string -> Result<ThemeMode * string, string>
val resolveThemeList: text: string -> Result<(ThemeMode * string) list, string>
