module SvgAuthoringTests

open System.Security.Cryptography
open System.Text
open System.IO
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

        test "studio primitives, radial gradients and inert renderer shapes import portably" {
            let xml = """<svg viewBox="0 0 40 40"><defs><radialGradient id="glow" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#000000"/></radialGradient></defs><circle id="c" cx="5" cy="5" r="4" fill="url(#glow)"/><ellipse id="e" cx="15" cy="5" rx="4" ry="2"/><line id="l" x1="0" y1="12" x2="20" y2="12" stroke="#000000"/><polygon id="p" points="0,20 8,20 4,28"/><polyline id="q" points="10,20 18,24 10,28" fill="none"/></svg>"""
            let imported = SvgImport.importXml (request "studio" SvgDocument.defaultLimits) xml |> Result.defaultWith (failtestf "%A")
            Expect.equal imported.Definitions.Length 1 "radial gradient imported"
            Expect.equal imported.Children.Length 5 "circle, ellipse, line, polygon and polyline imported"
            let exported = SvgDocument.exportSvg "studio-roundtrip" imported |> Result.defaultWith (failtestf "%A")
            let reopened = SvgImport.importXml (request "studio-reopen" SvgDocument.defaultLimits) exported |> Result.defaultWith (failtestf "%A")
            Expect.equal reopened.Children.Length imported.Children.Length "tool output reopens"
            match SvgImport.importXml (request "unsafe" SvgDocument.defaultLimits) (xml.Replace("</defs>", "<style>@font-face{src:url(data:x)}</style></defs>")) with
            | Error issues -> Expect.equal issues.Head.Code "active-content" "default import still refuses CSS/data resources"
            | Ok _ -> failtest "default import accepted CSS"
        }

        test "portable art edits preserve tool state and grouped history" {
            let empty = { Schema=SvgDocument.schema; Id="art"; ViewBox={X=0.0;Y=0.0;Width=100.0;Height=100.0}; Definitions=[]; Children=[] }
            let presentation = SvgDocument.defaultPresentation
            let rectangle = SvgArt.create "rect" (SvgArtPrimitive.Rectangle {X=1.0;Y=2.0;Width=10.0;Height=8.0}) presentation empty |> Result.defaultWith (failtestf "%A")
            let path = { Commands=[PathCommand.MoveTo {X=0.0;Y=0.0};PathCommand.QuadTo({X=5.0;Y=8.0},{X=10.0;Y=0.0});PathCommand.CubicTo({X=12.0;Y=2.0},{X=14.0;Y=2.0},{X=16.0;Y=0.0});PathCommand.Close]; FillType=PathFillType.Winding }
            let created = SvgArt.create "curve" (SvgArtPrimitive.Path path) presentation rectangle |> Result.defaultWith (failtestf "%A")
            let edited = SvgArt.insertPathPoint "curve" 1 {X=2.0;Y=1.0} created |> Result.bind (SvgArt.removePathPoint "curve" 1) |> Result.defaultWith (failtestf "%A")
            let grouped = SvgArt.group "pair" ["rect";"curve"] edited |> Result.bind (SvgArt.ungroup "pair") |> Result.defaultWith (failtestf "%A")
            let state = SvgAuthoring.tryCreate 0 empty (catalog []) [] |> Result.defaultWith (failtestf "%A")
            let gesture = { Schema=SvgAuthoring.transactionSchema; Id="pointer-1"; Operations=[SvgAuthoringOperation.ReplaceDocument grouped] }
            let previewed = SvgAuthoring.preview 0 gesture state |> Result.bind (SvgAuthoring.preview 0 gesture) |> Result.defaultWith (failtestf "%A")
            let committed = SvgAuthoring.commitPreview 0 gesture.Id previewed |> Result.defaultWith (failtestf "%A")
            Expect.equal committed.Undo.Length 1 "repeated gesture preview commits once"
            Expect.equal SvgArt.initialState.Camera SvgAffine.identity "camera is separate from accepted document history"
            Expect.equal (SvgArt.snapPoint SvgArt.initialState {X=12.0;Y=(-4.0)}) (Ok {X=16.0;Y=(-8.0)}) "grid snapping is deterministic"
            match SvgArt.translate ["missing"] 1.0 1.0 grouped with Error _ -> () | Ok _ -> failtest "invalid numeric selection changed content"
        }

        test "geometry preparation is bounded, adaptive and stale-safe" {
            let closed = { Commands=[PathCommand.MoveTo {X=0.0;Y=0.0};PathCommand.QuadTo({X=5.0;Y=10.0},{X=10.0;Y=0.0});PathCommand.LineTo {X=0.0;Y=0.0};PathCommand.Close]; FillType=PathFillType.Winding }
            let prepared = SvgGeometry.prepare "union-1" 7 PathOperation.Union [closed] [closed] SvgGeometry.defaultMaximumDeviation |> Result.defaultWith (failtestf "%A")
            Expect.isGreaterThan prepared.InputVertexCount 6 "curve is adaptively flattened"
            Expect.isLessThanOrEqual prepared.EncodedRequest.Length 1048576 "wire request is bounded"
            let result = {OperationId="union-1";AcceptedRevision=7;InputContentHash=prepared.Request.InputContentHash;Contours=[[{X=0.0;Y=0.0};{X=10.0;Y=0.0};{X=10.0;Y=10.0};{X=0.0;Y=0.0}]]}
            let empty = { Schema=SvgDocument.schema; Id="boolean"; ViewBox={X=0.0;Y=0.0;Width=20.0;Height=20.0}; Definitions=[]; Children=[] }
            Expect.isOk (SvgGeometry.transaction result prepared empty) "matching result produces one atomic transaction"
            match SvgGeometry.transaction {result with AcceptedRevision=8} prepared empty with Error(SvgArtError.InvalidInput issues) -> Expect.equal issues.Head.Code "stale-geometry-result" "stale worker result discarded" | value -> failtestf "stale result accepted: %A" value
            Expect.isError (SvgGeometry.prepare "bad" 0 PathOperation.Union [closed] [] 0.001) "deviation below 0.01 refuses"
            Expect.isError (SvgGeometry.prepare "open" 0 PathOperation.Union [{closed with Commands=closed.Commands |> List.filter ((<>) PathCommand.Close)}] [] 0.25) "open contours refuse"
            let degenerate = {Commands=[PathCommand.MoveTo {X=0.0;Y=0.0};PathCommand.LineTo {X=0.0;Y=0.0};PathCommand.Close];FillType=PathFillType.Winding}
            Expect.isError (SvgGeometry.prepare "degenerate" 0 PathOperation.Union [degenerate] [] 0.25) "degenerate contours refuse before worker dispatch"
        }

        test "asset revisions are immutable while identical reinsertion is harmless" {
            let document = imported "immutable"
            let original = asset "shape" 1 document []
            let state = SvgAuthoring.tryCreate 0 document (catalog [original]) [] |> Result.defaultWith (failtestf "%A")
            let transaction value id = {Schema=SvgAuthoring.transactionSchema;Id=id;Operations=[SvgAuthoringOperation.UpsertAsset value]}
            Expect.isOk (SvgAuthoring.commit 0 (transaction original "same") state) "identical revision reinsertion is accepted"
            let changed = { original with Rights = {original.Rights with License="MIT"} }
            match SvgAuthoring.commit 0 (transaction changed "changed") state with
            | Error(SvgAuthoringError.InvalidTransaction issues) -> Expect.equal issues.Head.Code "immutable-asset-revision" "changed rights require a new revision"
            | value -> failtestf "immutable revision was replaced: %A" value
        }

        test "verified Noto resource round trips detached and rejects altered bytes and manifests" {
            let base64 = File.ReadAllText(Path.Combine(__SOURCE_DIRECTORY__, "../../src/Scene.SvgBrowser/noto-sans-latin-400-normal.woff2.base64")).Replace("\n", "").Replace("\r", "")
            let resource = SvgResourceInterchange.notoSansLatin400 base64 |> Result.defaultWith (failtestf "%A")
            Expect.equal resource.Sha256 "09aee8065d25508f23a4c3d92cd777ac869c52d93fd868a88f025d888a7937d6" "approved exact bytes verified"
            let text =
                { Id="text";SemanticId=Some "text";Visible=true;Transform=SvgAffine.identity;ClipId=None;MaskId=None;Presentation=None
                  Content=SvgElementContent.SceneLeaf {Nodes=[SceneNode.TextRun {Text="Offline Noto";Position={X=2.0;Y=18.0};Font={Family=Some "Noto Sans";Size=16.0;Weight=Some 400};Paint={Fill=Some {Red=0uy;Green=0uy;Blue=0uy;Alpha=255uy};Stroke=None;Opacity=1.0;Antialias=true;BlendMode=BlendMode.SrcOver;Shader=None;ColorFilter=ColorFilter.NoColorFilter;MaskFilter=MaskFilter.NoMaskFilter;ImageFilter=ImageFilter.NoImageFilter;PathEffect=PathEffect.NoPathEffect}}]} }
            let font = {Id=resource.DefinitionId;Content=SvgDefinitionContent.Font {Family=resource.Family;Source=resource.FileName;Sha256=resource.Sha256;License=resource.License}}
            let document = {Schema=SvgDocument.schema;Id="font";ViewBox={X=0.0;Y=0.0;Width=100.0;Height=40.0};Definitions=[font];Children=[text]}
            let xml = SvgResourceInterchange.exportSvg "offline" {Document=document;Fonts=[resource]} |> Result.defaultWith (failtestf "%A")
            Expect.stringContains xml "data:font/woff2;base64," "resource export is self-contained"
            let restored = SvgResourceInterchange.importXml (request "offline" SvgDocument.defaultLimits) xml |> Result.defaultWith (failtestf "%A")
            Expect.equal restored.Fonts [resource] "resource returns detached with exact bytes"
            Expect.equal restored.Document.Children.Length 1 "typed text survives resource reopen"
            let altered = base64.Substring(0,base64.Length-4) + "AAAA"
            match SvgResourceInterchange.notoSansLatin400 altered with Error issues -> Expect.equal issues.Head.Code "font-hash-mismatch" "altered bytes refuse" | Ok _ -> failtest "altered font accepted"
            match SvgResourceInterchange.exportSvg "bad" {Document=document;Fonts=[{resource with License="MIT"}]} with Error issues -> Expect.equal issues.Head.Code "unapproved-font-manifest" "wrong rights refuse" | Ok _ -> failtest "wrong font rights accepted"
            let excessive = String.replicate 1398104 "A"
            match SvgResourceInterchange.notoSansLatin400 excessive with Error issues -> Expect.equal issues.Head.Code "font-byte-limit" "encoded size rejects before allocation" | Ok _ -> failtest "excessive font accepted"
        }
    ]
