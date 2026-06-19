module SecondAntShowcase.Core.AntTheme

open FS.GG.UI.DesignSystem
open SecondAntShowcase.Core.Model

val antLight: Theme
val antDark: Theme
val resolve: mode: ThemeMode -> Theme
val defaultTheme: Theme
val modeName: mode: ThemeMode -> string
