module Feature146PortableSceneResourceTests

open Expecto
open FS.GG.UI.Scene

let private resourceScenePackage () =
    SceneCodec.export (Scene.image (0.0, 0.0, 16.0, 16.0) "logo.png")

let private available hash =
    { ResourceId = "image-0001"
      Kind = Some ImageResource
      ContentHash = Some hash
      ByteLength = None
      Status = ResourceAvailable }

[<Tests>]
let feature146PortableSceneResourceTests =
    testList "Feature146 portable scene resource inspection" [
        test "required missing resource rejects package" {
            let package = resourceScenePackage ()
            let report = SceneCodec.inspectWith SceneCodec.defaultInspectionOptions package.CanonicalBytes
            Expect.equal report.Status PackageRejected "missing required resource rejects"
            Expect.exists report.Diagnostics (fun d -> d.Stage = Resource && d.ResourceId = Some "image-0001") "resource diagnostic names the missing resource"
        }

        test "resource hash mismatch rejects package" {
            let package = resourceScenePackage ()
            let options =
                { SceneCodec.defaultInspectionOptions with
                    Resources = [ available "sha256:not-the-package-hash" ] }

            let report = SceneCodec.inspectWith options package.CanonicalBytes
            Expect.equal report.Status PackageRejected "hash mismatch rejects"
            Expect.exists report.Diagnostics (fun d -> d.Message.Contains("metadata does not match")) "metadata mismatch is diagnostic"
        }

        test "matching resource accepts package" {
            let package = resourceScenePackage ()
            let options =
                { SceneCodec.defaultInspectionOptions with
                    Resources = [ available package.Resources.Head.ContentHash ] }

            let report = SceneCodec.inspectWith options package.CanonicalBytes
            Expect.equal report.Status PackageAccepted "matching resource accepts"
        }
    ]
