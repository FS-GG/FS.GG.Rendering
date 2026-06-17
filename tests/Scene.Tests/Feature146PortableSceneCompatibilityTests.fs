module Feature146PortableSceneCompatibilityTests

open System
open System.IO
open System.Text
open Expecto
open FS.GG.UI.Scene

let private packageBytes () =
    let package = SceneCodec.export (Scene.rectangle (0.0, 0.0, 10.0, 10.0) Colors.white)
    package.CanonicalBytes

let private appendSection (tag: int) (payload: byte[]) (bytes: byte[]) =
    use stream = new MemoryStream()
    stream.Write(bytes, 0, bytes.Length)
    use writer = new BinaryWriter(stream, Encoding.UTF8, true)
    writer.Write(tag)
    writer.Write(payload.Length)
    writer.Write(payload)
    writer.Flush()
    stream.ToArray()

let private rewriteMajorVersion (major: int) (bytes: byte[]) =
    let copy = Array.copy bytes
    BitConverter.GetBytes(major).CopyTo(copy, SceneCodec.magicHeader.Length)
    copy

[<Tests>]
let feature146PortableSceneCompatibilityTests =
    testList "Feature146 portable scene compatibility" [
        test "malformed package is rejected" {
            let report = SceneCodec.inspect [| 0uy; 1uy; 2uy |]
            Expect.equal report.Status PackageRejected "short package is rejected"
            Expect.isNonEmpty report.Diagnostics "diagnostic explains malformed input"
        }

        test "newer major version is rejected before rendering" {
            let report = packageBytes () |> rewriteMajorVersion 2 |> SceneCodec.inspect
            Expect.equal report.Status PackageRejected "newer major version rejects"
            Expect.exists report.Diagnostics (fun d -> d.Stage = Version) "version diagnostic is reported"
        }

        test "unknown required tag rejects the package" {
            let bytes = packageBytes () |> appendSection 99 Array.empty
            let report = SceneCodec.inspect bytes
            Expect.equal report.Status PackageRejected "unknown required tag rejects"
            Expect.exists report.Diagnostics (fun d -> d.Stage = Parse) "parse diagnostic is reported"
        }

        test "unknown optional tag is skipped and reported" {
            let bytes = packageBytes () |> appendSection 1000 Array.empty
            let report = SceneCodec.inspect bytes
            Expect.equal report.Status PackageAcceptedWithDegradation "optional tag is skipped with warning"
            Expect.exists report.Diagnostics (fun d -> d.Severity = Warning && d.Stage = Parse) "optional tag warning is reported"
        }
    ]
