namespace FS.GG.UI.Themes.AntDesign

open FS.GG.UI.DesignSystem

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
/// The Ant intent policy: makes primary/default/dashed/text/link/danger visually distinct,
/// driving intent divergence through the StyleResolver front-half seam with no control forks.
module AntIntentPolicy =
    /// The Ant `IntentPolicy` (use with `StyleResolver.resolve` to style Ant intents). Total over
    /// every intent string: `""`/unknown resolve to identity (the structural base unchanged).
    val policy: StyleResolver.IntentPolicy
