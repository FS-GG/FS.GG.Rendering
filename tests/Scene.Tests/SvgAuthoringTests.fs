module SvgAuthoringTests

open System.Security.Cryptography
open System.Text
open Expecto
open FS.GG.UI.Scene

let private richSvg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
  <defs>
    <linearGradient id="paint"><stop offset="0" stop-color="#ff0000"/><stop offset="1" stop-color="rgb(0 0 255)"/></linearGradient>
    <clipPath id="crop"><rect x="0" y="0" width="80" height="80"/></clipPath>
    <mask id="fade" x="0" y="0" width="100" height="100"><rect id="mask-shape" x="0" y="0" width="100" height="100" fill="white"/></mask>
    <symbol id="badge" viewBox="0 0 10 10"><path id="symbol-path" d="M 0 0 L 10 0 L 10 10 Z" fill="#ffffff"/></symbol>
  </defs>
  <rect id="shared" data-semantic-id="hero" x="4" y="5" width="20" height="12" fill="url(#paint)" clip-path="url(#crop)" mask="url(#fade)"/>
  <path id="route" d="M 1 2 C 3 4 5 6 7 8 Q 9 10 11 12 Z" fill="#112233" fill-rule="evenodd"/>
  <use id="instance" href="#badge"/>
</svg>"""

let private request assetNamespace limits =
    { AssetNamespace = assetNamespace; DocumentId = "fixture"; Limits = limits }

let private imported namespaceValue =
    match SvgImport.importXml (request namespaceValue SvgDocument.defaultLimits) richSvg with
    | Ok document -> document
    | Error issues -> failtestf "import failed: %A" issues

let private hash document =
    match SvgAsset.contentHash document with Ok value -> value | Error issues -> failtestf "hash failed: %A" issues

let private asset assetId revision document dependencies =
    { Schema = SvgAsset.schema
      AssetId = assetId
      Revision = revision
      ContentHash = hash document
      Rights = { License = "CC0-1.0"; Attribution = None; Source = Some "fixture" }
      Dependencies = dependencies
      Document = document }

let private catalog assets = { Schema = SvgAsset.catalogSchema; Assets = assets }

[<Tests>]
let tests =
    testList "SVG authoring boundary" [
        test "supported SVG imports with asset-local identities and canonical SHA-256" {
            let first = imported "asset-a"
            let second = imported "asset-b"
            Expect.equal first.Definitions.Length 4 "gradient, clip, mask and symbol imported"
            Expect.equal first.Children.Length 3 "rectangle, path and symbol instance imported in order"
            Expect.isTrue (first.Children.Head.Id.StartsWith("asset-a--")) "source ids are namespaced"
            Expect.notEqual first.Children.Head.Id second.Children.Head.Id "colliding source ids remain asset-local"
            let serialized = SvgDocument.serialize first |> Result.defaultWith (failtestf "%A")
            let expected = SHA256.HashData(Encoding.UTF8.GetBytes serialized) |> System.Convert.ToHexString |> fun value -> value.ToLowerInvariant()
            Expect.equal (hash first) expected "portable hash is real SHA-256 over canonical bytes"
        }

        test "supported export and reimport preserve geometry order and presentation" {
            let first = imported "source"
            let exported = SvgDocument.exportSvg "roundtrip" first |> Result.defaultWith (failtestf "%A")
            let restored = SvgImport.importXml (request "restored" SvgDocument.defaultLimits) exported |> Result.defaultWith (failtestf "%A")
            Expect.equal restored.ViewBox first.ViewBox "viewBox survives"
            Expect.equal restored.Children.Length first.Children.Length "top-level order survives"
            Expect.equal (restored.Children |> List.map (fun value -> value.Presentation.IsSome)) (first.Children |> List.map (fun value -> value.Presentation.IsSome)) "presentation remains explicit"
            let sceneKinds (document: SvgDocument) = document.Children |> List.map (fun value -> match value.Content with SvgElementContent.Group _ -> "group" | SvgElementContent.SceneLeaf { Nodes = SceneNode.PaintedRectangle _ :: _ } -> "rect" | SvgElementContent.SceneLeaf { Nodes = SceneNode.Path _ :: _ } -> "path" | SvgElementContent.SymbolInstance _ -> "use" | _ -> "other")
            Expect.equal (sceneKinds restored) [ "group"; "group"; "group" ] "export wrappers preserve top-level order"
        }

        test "active malformed over-budget and external XML fail with locations" {
            let cases =
                [ "active", richSvg.Replace("<path id=\"route\"", "<script/><path id=\"route\""), "active-content"
                  "event", richSvg.Replace("id=\"shared\"", "id=\"shared\" onclick=\"run()\""), "active-content"
                  "external", richSvg.Replace("href=\"#badge\"", "href=\"https://example.test/badge.svg#x\""), "external-reference"
                  "malformed", richSvg.Replace("</svg>", ""), "malformed-xml"
                  "dtd", "<!DOCTYPE svg><svg viewBox=\"0 0 1 1\"/>", "active-content" ]
            for name, xml, code in cases do
                match SvgImport.importXml (request "bad" SvgDocument.defaultLimits) xml with
                | Ok _ -> failtestf "%s unexpectedly imported" name
                | Error issues ->
                    Expect.equal issues.Head.Code code $"{name} code"
                    Expect.isTrue (issues.Head.Location.StartsWith("/")) $"{name} is located"
            let tiny = { SvgDocument.defaultLimits with MaxSerializedBytes = 8 }
            match SvgImport.importXml (request "bad" tiny) richSvg with
            | Error issues -> Expect.equal issues.Head.Code "document-byte-limit" "budget checked before parse"
            | Ok _ -> failtest "over-budget SVG imported"
            let oneSegment = { SvgDocument.defaultLimits with MaxPathSegments = 1 }
            match SvgImport.importXml (request "bad" oneSegment) richSvg with
            | Error issues -> Expect.equal issues.Head.Code "path-segment-limit" "path budget stops token expansion"
            | Ok _ -> failtest "over-segment SVG imported"
        }

        test "asset schema hash references and cycles fail closed" {
            let document = imported "asset"
            let valid = asset "shape" 1 document []
            Expect.isEmpty (SvgAsset.validateCatalog (catalog [ valid ])) "valid asset accepted"
            let serialized = SvgAsset.serializeCatalog (catalog [ valid ]) |> Result.defaultWith (failtestf "%A")
            Expect.equal (SvgAsset.deserializeCatalog serialized) (Ok(catalog [ valid ])) "catalog wire round trip is canonical"
            match SvgAsset.deserializeCatalog (serialized.Replace(SvgAsset.catalogSchema, "fsgg.svg-asset-catalog/9")) with
            | Error issues -> Expect.exists issues (fun value -> value.Code = "unknown-catalog-schema") "wire schema version is checked"
            | Ok _ -> failtest "unknown wire schema imported"
            let wrong = { valid with ContentHash = String.replicate 64 "0" }
            Expect.exists (SvgAsset.validateCatalog (catalog [ wrong ])) (fun value -> value.Code = "content-hash-mismatch") "hash mismatch diagnosed"
            let missing = { valid with Dependencies = [ { AssetId = "missing"; Revision = 1 } ] }
            Expect.exists (SvgAsset.validateCatalog (catalog [ missing ])) (fun value -> value.Code = "missing-asset-reference") "missing dependency diagnosed"
            let first = { valid with AssetId = "a"; Dependencies = [ { AssetId = "b"; Revision = 1 } ] }
            let second = { valid with AssetId = "b"; Dependencies = [ { AssetId = "a"; Revision = 1 } ] }
            Expect.exists (SvgAsset.validateCatalog (catalog [ first; second ])) (fun value -> value.Code = "cyclic-asset-reference") "cycle diagnosed"
            Expect.exists (SvgAsset.validateCatalog { catalog [ valid ] with Schema = "fsgg.svg-asset-catalog/99" }) (fun value -> value.Code = "unknown-catalog-schema") "unknown version diagnosed"
            let overBudget = [ for revision in 1 .. SvgDocument.defaultLimits.MaxDefinitions + 1 -> { valid with Revision = revision } ]
            Expect.exists (SvgAsset.validateCatalog (catalog overBudget)) (fun value -> value.Code = "asset-limit") "catalog allocation budget is enforced"
        }

        test "shared revision update preserves overrides and reports removed targets" {
            let revisionOne = imported "prefab"
            let revisionTwo = { revisionOne with Children = revisionOne.Children.Tail }
            let assets = catalog [ asset "shape" 1 revisionOne []; asset "shape" 2 revisionTwo [] ]
            let overrideValue = { ElementId = revisionOne.Children.Head.Id; Property = SvgPrefabProperty.Visibility; Value = SvgPrefabOverrideValue.Visibility false }
            let instances =
                [ { InstanceId = "one"; AssetId = "shape"; AcceptedRevision = 1; Overrides = [ overrideValue ] }
                  { InstanceId = "two"; AssetId = "shape"; AcceptedRevision = 1; Overrides = [] } ]
            let state = SvgAuthoring.tryCreate 0 revisionOne assets instances |> Result.defaultWith (failtestf "%A")
            let resolved, conflicts = SvgAsset.resolveInstance assets instances.Head |> Result.defaultWith (failtestf "%A")
            Expect.isFalse resolved.Children.Head.Visible "typed override applies over inherited value"
            Expect.isEmpty conflicts "accepted revision has no conflict"
            let transaction = { Schema = SvgAuthoring.transactionSchema; Id = "upgrade"; Operations = [ SvgAuthoringOperation.UpdateInstances("shape", 1, 2) ] }
            let updated = SvgAuthoring.commit 0 transaction state |> Result.defaultWith (failtestf "%A")
            Expect.equal (updated.Instances |> List.map _.AcceptedRevision) [ 2; 2 ] "shared instances update explicitly"
            Expect.equal updated.Instances.Head.Overrides [ overrideValue ] "override remains distinguishable"
            Expect.equal updated.Conflicts.Length 1 "removed overridden element produces a reviewable conflict"
        }

        test "grouped preview commits once; cancellation stale and invalid work are atomic" {
            let document = imported "edit"
            let assets = catalog [ asset "shape" 1 document [] ]
            let state = SvgAuthoring.tryCreate 4 document assets [] |> Result.defaultWith (failtestf "%A")
            let ids = document.Children |> List.take 2 |> List.map _.Id
            let gesture = { Schema = SvgAuthoring.transactionSchema; Id = "brush-gesture"; Operations = [ SvgAuthoringOperation.TransformElements(ids, SvgAffine.translate 2.0 3.0); SvgAuthoringOperation.TransformElements(ids, SvgAffine.translate 1.0 0.0) ] }
            let previewed = SvgAuthoring.preview 4 gesture state |> Result.defaultWith (failtestf "%A")
            Expect.equal previewed.Document state.Document "preview does not mutate accepted content"
            Expect.isEmpty previewed.Undo "preview does not mutate history"
            let cancelled = SvgAuthoring.cancelPreview gesture.Id previewed |> Result.defaultWith (failtestf "%A")
            Expect.equal cancelled state "cancellation restores exact state"
            let committed = SvgAuthoring.preview 4 gesture state |> Result.bind (SvgAuthoring.commitPreview 4 gesture.Id) |> Result.defaultWith (failtestf "%A")
            Expect.equal committed.Revision 5 "group advances one revision"
            Expect.equal committed.Undo.Length 1 "multi-object multi-step gesture creates one undo entry"
            let undone = SvgAuthoring.undo 5 committed |> Result.defaultWith (failtestf "%A")
            Expect.equal undone.Document state.Document "undo restores whole transaction"
            let redone = SvgAuthoring.redo 6 undone |> Result.defaultWith (failtestf "%A")
            Expect.equal redone.Document committed.Document "redo restores whole transaction"
            let newEdit = SvgAuthoring.commit 7 { gesture with Id = "new-edit" } redone |> Result.defaultWith (failtestf "%A")
            Expect.isEmpty newEdit.Redo "new edit clears redo"
            match SvgAuthoring.commit 4 gesture committed with Error(SvgAuthoringError.StaleRevision(4, 5)) -> () | value -> failtestf "stale completion changed state: %A" value
            let invalid = { Schema = SvgAuthoring.transactionSchema; Id = "invalid"; Operations = [ SvgAuthoringOperation.TransformElements([ "missing" ], SvgAffine.identity) ] }
            match SvgAuthoring.commit 5 invalid committed with Error(SvgAuthoringError.InvalidTransaction _) -> () | value -> failtestf "invalid operation accepted: %A" value
            Expect.equal committed.Undo.Length 1 "failed operation cannot add history"
            match SvgAuthoring.commit 5 { gesture with Schema = "fsgg.svg-authoring-transaction/99" } committed with Error(SvgAuthoringError.InvalidTransaction issues) -> Expect.equal issues.Head.Code "unknown-authoring-schema" "unknown transaction schema refuses" | value -> failtestf "unknown transaction schema committed: %A" value

            let first = asset "a" 1 document [ { AssetId = "b"; Revision = 1 } ]
            let second = asset "b" 1 document [ { AssetId = "a"; Revision = 1 } ]
            let cyclic = { Schema = SvgAuthoring.transactionSchema; Id = "cycle"; Operations = [ SvgAuthoringOperation.UpsertAsset first; SvgAuthoringOperation.UpsertAsset second ] }
            match SvgAuthoring.commit 5 cyclic committed with Error(SvgAuthoringError.InvalidTransaction issues) -> Expect.exists issues (fun value -> value.Code = "cyclic-asset-reference") "cycle refuses atomic commit" | value -> failtestf "cyclic catalog committed: %A" value
            let missingAsset = asset "missing-owner" 1 document [ { AssetId = "absent"; Revision = 1 } ]
            let missing = { Schema = SvgAuthoring.transactionSchema; Id = "missing"; Operations = [ SvgAuthoringOperation.UpsertAsset missingAsset ] }
            match SvgAuthoring.commit 5 missing committed with Error(SvgAuthoringError.InvalidTransaction issues) -> Expect.exists issues (fun value -> value.Code = "missing-asset-reference") "missing reference refuses atomic commit" | value -> failtestf "missing reference committed: %A" value
        }

        test "play snapshot remains immutable across later edits" {
            let document = imported "snapshot"
            let state = SvgAuthoring.tryCreate 0 document (catalog []) [] |> Result.defaultWith (failtestf "%A")
            let snapshotted = SvgAuthoring.takePlaySnapshot 0 state |> Result.defaultWith (failtestf "%A")
            let replacement = { document with Id = "later" }
            let changed = SvgAuthoring.commit 0 { Schema = SvgAuthoring.transactionSchema; Id = "later"; Operations = [ SvgAuthoringOperation.ReplaceDocument replacement ] } snapshotted |> Result.defaultWith (failtestf "%A")
            Expect.equal changed.PlaySnapshot snapshotted.PlaySnapshot "later edit cannot mutate snapshot"
            Expect.equal (SvgDocument.deserialize changed.PlaySnapshot.Value.SerializedDocument) (Ok document) "snapshot owns canonical bytes"
        }
    ]
