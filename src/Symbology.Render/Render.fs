namespace FS.GG.UI.Symbology.Render

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module Render =

    let toPng (size: Size) (scene: Scene) (dir: string) : string =
        // Public path only: SceneCodec round-trip -> the public ReferenceRendering oracle.
        // SceneRenderer is internal, so this reaches no internal entry (FR-010).
        let bytes = (SceneCodec.export scene).CanonicalBytes

        let request =
            { PackageBytes = bytes
              OutputDirectory = dir
              OutputSize = size
              Resources = [] }

        let ev = ReferenceRendering.run request

        match ev.Verdict, ev.ImagePath with
        | ReferencePassed, Some path -> path
        | verdict, image ->
            // Fail loud — the three-case verdict means only ReferencePassed-with-a-path is success.
            let diagnostics = ev.Diagnostics |> String.concat "; "

            failwithf
                "Symbology.Render.toPng: render did not pass (verdict %A, imagePath %A): %s"
                verdict
                image
                diagnostics
