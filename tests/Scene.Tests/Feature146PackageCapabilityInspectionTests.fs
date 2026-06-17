module Feature146PackageCapabilityInspectionTests

open Expecto
open FS.GG.UI.Scene

let private package =
    let scene =
        Scene.group
            [ Scene.rectangle (0.0, 0.0, 20.0, 20.0) Colors.white
              Scene.glyphRunProof { X = 0.0; Y = 24.0 } "text" { Family = None; Size = 12.0; Weight = None } (Paint.fill Colors.black) ]

    SceneCodec.exportScene
        { SceneCodec.defaultExportOptions with
            OptionalCapabilities = [ "scene.glyph-run" ] }
        scene

[<Tests>]
let feature146PackageCapabilityInspectionTests =
    testList "Feature146 package capability inspection" [
        test "required unsupported capability rejects" {
            let profile =
                { ProfileId = "rect-only"
                  SupportedCapabilities = [ "scene.group"; "scene.rectangle" ] }

            let report =
                SceneCodec.inspectWith
                    { SceneCodec.defaultInspectionOptions with TargetProfile = Some profile }
                    package.CanonicalBytes

            Expect.equal report.Status PackageAcceptedWithDegradation "optional glyph-run can degrade when required capabilities are supported"
            Expect.exists report.CapabilityVerdicts (fun verdict -> verdict.Requirement.CapabilityId = "scene.glyph-run" && verdict.Degraded) "glyph run is degraded"
        }

        test "blocking required capability names affected path" {
            let profile =
                { ProfileId = "no-rect"
                  SupportedCapabilities = [ "scene.group"; "scene.glyph-run" ] }

            let report =
                SceneCodec.inspectWith
                    { SceneCodec.defaultInspectionOptions with TargetProfile = Some profile }
                    package.CanonicalBytes

            Expect.equal report.Status PackageRejected "required rectangle rejection blocks package"
            Expect.exists report.Diagnostics (fun d -> d.CapabilityId = Some "scene.rectangle" && d.ScenePath.IsSome) "affected scene path is diagnostic"
        }
    ]
