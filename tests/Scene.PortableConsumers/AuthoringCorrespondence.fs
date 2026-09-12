module AuthoringCorrespondence

open FS.GG.UI.Scene

let private fixture = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><defs><linearGradient id="paint"><stop offset="0" stop-color="#ff0000"/><stop offset="1" stop-color="#0000ff"/></linearGradient><clipPath id="clip"><rect x="0" y="0" width="30" height="30"/></clipPath><mask id="mask" x="0" y="0" width="32" height="32"><rect id="mask-body" x="0" y="0" width="32" height="32" fill="white"/></mask><symbol id="symbol" viewBox="0 0 4 4"><path id="symbol-path" d="M 0 0 L 4 0 L 4 4 Z" fill="white"/></symbol></defs><rect id="box" x="1" y="2" width="8" height="9" fill="url(#paint)" clip-path="url(#clip)" mask="url(#mask)"/><path id="route" d="M 2 3 C 4 5 6 7 8 9 Z" fill="#123456"/><use id="copy" href="#symbol"/></svg>"""

let private code (result: Result<SvgDocument, SvgDocumentIssue list>) = match result with Ok _ -> "accepted" | Error issues -> issues.Head.Code

let run runtime =
    let request = { AssetNamespace = "portable"; DocumentId = "portable-authoring"; Limits = SvgDocument.defaultLimits }
    let document = SvgImport.importXml request fixture |> Result.defaultWith (fun issues -> failwith $"portable import failed: {issues}")
    let serialized = SvgDocument.serialize document |> Result.defaultWith (fun issues -> failwith $"portable serialization failed: {issues}")
    let digest = SvgAsset.contentHash document |> Result.defaultWith (fun issues -> failwith $"portable hash failed: {issues}")
    let asset =
        { Schema = SvgAsset.schema; AssetId = "shape"; Revision = 1; ContentHash = digest
          Rights = { License = "CC0-1.0"; Attribution = None; Source = None }; Dependencies = []; Document = document }
    let catalog = { Schema = SvgAsset.catalogSchema; Assets = [ asset ] }
    let catalogWire = SvgAsset.serializeCatalog catalog |> Result.defaultWith (fun issues -> failwith $"portable catalog serialization failed: {issues}")
    let restoredCatalog = SvgAsset.deserializeCatalog catalogWire |> Result.defaultWith (fun issues -> failwith $"portable catalog deserialization failed: {issues}")
    let state = SvgAuthoring.tryCreate 0 document restoredCatalog [] |> Result.defaultWith (fun error -> failwith $"portable state failed: {error}")
    let ids = document.Children |> List.map _.Id
    let transaction = { Schema = SvgAuthoring.transactionSchema; Id = "gesture"; Operations = [ SvgAuthoringOperation.TransformElements(ids, SvgAffine.translate 2.0 3.0) ] }
    let committed = SvgAuthoring.commit 0 transaction state |> Result.defaultWith (fun error -> failwith $"portable commit failed: {error}")
    let undone = SvgAuthoring.undo 1 committed |> Result.defaultWith (fun error -> failwith $"portable undo failed: {error}")
    let redone = SvgAuthoring.redo 2 undone |> Result.defaultWith (fun error -> failwith $"portable redo failed: {error}")
    let security =
        [ fixture.Replace("<path id=\"route\"", "<script/><path id=\"route\"")
          fixture.Replace("href=\"#symbol\"", "href=\"https://example.test/x.svg#symbol\"")
          fixture.Replace("<svg ", "<svg onclick=\"run()\" ") ]
        |> List.map (SvgImport.importXml request >> code)
    let stale = match SvgAuthoring.commit 0 transaction committed with Error(SvgAuthoringError.StaleRevision _) -> "stale" | _ -> "mutant-survived"
    let invalid = match SvgAuthoring.commit 1 { Schema = SvgAuthoring.transactionSchema; Id = "invalid"; Operations = [ SvgAuthoringOperation.TransformElements([ "missing" ], SvgAffine.identity) ] } committed with Error(SvgAuthoringError.InvalidTransaction _) -> "atomic" | _ -> "mutant-survived"
    let unknownCatalog =
        match SvgAsset.deserializeCatalog(catalogWire.Replace(SvgAsset.catalogSchema, "fsgg.svg-asset-catalog/9")) with
        | Error issues -> issues.Head.Code
        | Ok _ -> "mutant-survived"
    let unknownTransaction =
        match SvgAuthoring.commit 1 { transaction with Schema = "fsgg.svg-authoring-transaction/9" } committed with
        | Error(SvgAuthoringError.InvalidTransaction issues) -> issues.Head.Code
        | _ -> "mutant-survived"
    let canonical = String.concat "|" [ serialized; catalogWire; digest; string committed.Revision; string committed.Undo.Length; string undone.Redo.Length; string redone.Revision; String.concat "," security; stale; invalid; unknownCatalog; unknownTransaction ]
    let negativeSummary = String.concat "," security
    printfn $"authoring-correspondence: runtime={runtime} definitions={document.Definitions.Length} children={document.Children.Length} negatives={negativeSummary},{stale},{invalid},{unknownCatalog},{unknownTransaction}"
    canonical
