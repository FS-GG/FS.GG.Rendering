module Feature146PackageResourceInspectionTests

open Expecto
open FS.GG.UI.Scene

let private package =
    let resource =
        { ResourceId = "optional-font"
          Kind = FontResource
          ContentHash = "sha256:font"
          ByteLength = Some 128L
          Required = false
          MediaType = Some "font/ttf"
          SourceLabel = Some "Bundled demo font" }

    SceneCodec.exportScene
        { SceneCodec.defaultExportOptions with Resources = [ resource ] }
        (Scene.textAt { X = 0.0; Y = 20.0 } "fallback text" Colors.black)

[<Tests>]
let feature146PackageResourceInspectionTests =
    testList "Feature146 package resource availability inspection" [
        test "optional missing resource degrades package" {
            let report = SceneCodec.inspect package.CanonicalBytes
            Expect.equal report.Status PackageAcceptedWithDegradation "optional missing resource degrades"
            Expect.exists report.ResourceVerdicts (fun verdict -> verdict.Entry.ResourceId = "optional-font" && verdict.Degraded) "optional resource verdict degrades"
        }

        test "duplicate availability rejects package" {
            let availability =
                { ResourceId = "optional-font"
                  Kind = Some FontResource
                  ContentHash = Some "sha256:font"
                  ByteLength = Some 128L
                  Status = ResourceAvailable }

            let report =
                SceneCodec.inspectWith
                    { SceneCodec.defaultInspectionOptions with Resources = [ availability; availability ] }
                    package.CanonicalBytes

            Expect.equal report.Status PackageRejected "duplicated resource evidence rejects"
            Expect.exists report.Diagnostics (fun d -> d.ResourceId = Some "optional-font") "duplicate diagnostic names resource"
        }
    ]
