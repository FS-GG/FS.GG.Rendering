module SecondAntShowcase.Core.Shell

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open SecondAntShowcase.Core.Model

val view: size: Size -> model: SecondAntShowcaseModel -> Control<SecondAntShowcaseMsg>
