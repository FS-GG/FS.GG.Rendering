module Product.LayoutEvidence

open FS.GG.UI.Scene
open Product.Model
open Product.View
//#if (profile == "governed" || profile == "headless-scene")

let layoutEvidenceForSize (size: Size) (model: Model) : LayoutEvidenceReport =
    let hud: LayoutRegionEvidence =
        { Name = "summary"
          Bounds = { X = 0.0; Y = 0.0; Width = float size.Width; Height = 64.0 } }

    let gameplay: LayoutRegionEvidence =
        { Name = "content"
          Bounds =
            { X = 0.0
              Y = hud.Bounds.Height
              Width = float size.Width
              Height = max 1.0 (float size.Height - hud.Bounds.Height) } }

    { Scene = { Nodes = [ view model ] }
      OutputSize = size
      ProofLevel = ReadableLayout
      HudRegion = Some hud
      GameplayRegion = Some gameplay
      TextBounds =
        [ { Name = "title"
            Text = $"Governed headless scene: {model.Name}"
            Bounds = { X = 32.0; Y = 40.0; Width = 240.0; Height = 24.0 }
            MeasurementMode = ApproximateTextBounds } ]
      GameplayBounds =
        [ { Name = "scene-content"
            Bounds = { X = 16.0; Y = 80.0; Width = 288.0; Height = 64.0 } } ]
      OverlapStatus = NoLayoutOverlap
      MeasurementMode = ApproximateTextBounds
      UnsupportedReasons = []
      Diagnostics = []
      RenderEvidence = None }

//#else
//#if (profile == "game")
// GAME family (feature 220): re-point the durable layout-evidence spine onto the game model —
// HUD region → the score strip, gameplay region → the playfield, active item → the ball. The
// evidence tokens (hud-region / gameplay-region / measurement-mode / overlap) are unchanged, so
// the durable governance scans and evidence commands keep passing across a starter swap.

let hudRegionForSize (size: Size) : LayoutRegionEvidence =
    { Name = "score"
      Bounds = { X = 0.0; Y = 0.0; Width = float size.Width; Height = 96.0 } }

let gameplayRegionForSize (size: Size) : LayoutRegionEvidence =
    let score = hudRegionForSize size

    { Name = "playfield"
      Bounds =
        { X = 0.0
          Y = score.Bounds.Height
          Width = float size.Width
          Height = max 1.0 (float size.Height - score.Bounds.Height) } }

let boundsInside outer inner =
    inner.X >= outer.X
    && inner.Y >= outer.Y
    && inner.X + inner.Width <= outer.X + outer.Width
    && inner.Y + inner.Height <= outer.Y + outer.Height

let private intersects first second =
    first.X < second.X + second.Width
    && first.X + first.Width > second.X
    && first.Y < second.Y + second.Height
    && first.Y + first.Height > second.Y

let private ballExtent = 16.0

// Map the ball's playfield position into the gameplay region, clamped so the active item is
// always inside the region (the durable movement/spawn/collision policies read this).
let activeGameplayBoundsForSize (size: Size) (model: Model) : LayoutGameplayBounds =
    let region = gameplayRegionForSize size
    let scaleX = region.Bounds.Width / model.PlayfieldWidth
    let scaleY = region.Bounds.Height / model.PlayfieldHeight
    let rawX = region.Bounds.X + model.Ball.CenterX * scaleX - ballExtent / 2.0
    let rawY = region.Bounds.Y + model.Ball.CenterY * scaleY - ballExtent / 2.0
    let x = rawX |> max region.Bounds.X |> min (region.Bounds.X + region.Bounds.Width - ballExtent)
    let y = rawY |> max region.Bounds.Y |> min (region.Bounds.Y + region.Bounds.Height - ballExtent)

    { Name = "active-item"
      Bounds = { X = x; Y = y; Width = ballExtent; Height = ballExtent } }

let movementUsesGameplayRegion size model =
    let region = gameplayRegionForSize size
    let bounds = activeGameplayBoundsForSize size model
    boundsInside region.Bounds bounds.Bounds

let spawnUsesGameplayRegion size model =
    let region = gameplayRegionForSize size
    let bounds = activeGameplayBoundsForSize size { model with Ball = initialModel.Ball }
    boundsInside region.Bounds bounds.Bounds

let collisionUsesGameplayRegion size model =
    movementUsesGameplayRegion size model

let private scoreTextBounds (size: Size) model =
    [ { Name = "tick"
        Text = $"tick: {model.TickCount}"
        Bounds = { X = 16.0; Y = 16.0; Width = 120.0; Height = 24.0 }
        MeasurementMode = ApproximateTextBounds }
      { Name = "score"
        Text = $"{model.LeftScore} : {model.RightScore}"
        Bounds = { X = float size.Width / 2.0 - 28.0; Y = 16.0; Width = 96.0; Height = 24.0 }
        MeasurementMode = ApproximateTextBounds } ]

let private overlapDiagnostics (report: LayoutEvidenceReport) =
    let hudTextOverlaps =
        report.TextBounds
        |> List.mapi (fun index first ->
            report.TextBounds
            |> List.skip (index + 1)
            |> List.choose (fun second ->
                if intersects first.Bounds second.Bounds then
                    Some
                        { Kind = HudTextOverlap
                          FirstName = first.Name
                          SecondName = Some second.Name
                          Bounds = first.Bounds
                          Message = $"HUD text '{first.Name}' overlaps '{second.Name}'" }
                else
                    None))
        |> List.concat

    let hudGameplayOverlaps =
        report.TextBounds
        |> List.collect (fun text ->
            report.GameplayBounds
            |> List.choose (fun gameplay ->
                if intersects text.Bounds gameplay.Bounds then
                    Some
                        { Kind = HudGameplayOverlap
                          FirstName = text.Name
                          SecondName = Some gameplay.Name
                          Bounds = text.Bounds
                          Message = $"HUD text '{text.Name}' overlaps gameplay '{gameplay.Name}'" }
                else
                    None))

    hudTextOverlaps @ hudGameplayOverlaps

let layoutEvidenceForSize size model : LayoutEvidenceReport =
    let report =
        { Scene = { Nodes = [ view model ] }
          OutputSize = size
          ProofLevel = ReadableLayout
          HudRegion = Some(hudRegionForSize size)
          GameplayRegion = Some(gameplayRegionForSize size)
          TextBounds = scoreTextBounds size model
          GameplayBounds = [ activeGameplayBoundsForSize size model ]
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = ApproximateTextBounds
          UnsupportedReasons = []
          Diagnostics = [ "hud-region=present"; "gameplay-region=present"; "measurement-mode=approximate" ]
          RenderEvidence = None }

    let overlaps = overlapDiagnostics report

    if overlaps.IsEmpty then
        report
    else
        { report with
            ProofLevel = DeterministicRenderOnly
            OverlapStatus = LayoutOverlaps overlaps
            Diagnostics = report.Diagnostics @ (overlaps |> List.map _.Message) }

let validateGeneratedLayout (report: LayoutEvidenceReport) =
    let overlaps = overlapDiagnostics report

    let diagnostics =
        [ if report.HudRegion.IsNone then
              "missing HUD region"
          if report.GameplayRegion.IsNone then
              "missing gameplay region"
          if report.TextBounds.IsEmpty then
              "missing HUD text bounds"
          if report.GameplayBounds.IsEmpty then
              "missing gameplay bounds"
          for overlap in overlaps do
              overlap.Message ]

    if diagnostics.IsEmpty then
        { Accepted = true
          FailureClass = None
          Diagnostics = [] }
    else
        { Accepted = false
          FailureClass = if overlaps.IsEmpty then Some MissingLayoutFacts else Some OverlappingLayoutBounds
          Diagnostics = diagnostics }

//#else

let hudRegionForSize (size: Size) : LayoutRegionEvidence =
    { Name = "summary"
      Bounds = { X = 0.0; Y = 0.0; Width = float size.Width; Height = 96.0 } }

let gameplayRegionForSize (size: Size) : LayoutRegionEvidence =
    let summary = hudRegionForSize size
    { Name = "content"
      Bounds =
        { X = 0.0
          Y = summary.Bounds.Height
          Width = float size.Width
          Height = max 1.0 (float size.Height - summary.Bounds.Height) } }

let boundsInside outer inner =
    inner.X >= outer.X
    && inner.Y >= outer.Y
    && inner.X + inner.Width <= outer.X + outer.Width
    && inner.Y + inner.Height <= outer.Y + outer.Height

let private intersects first second =
    first.X < second.X + second.Width
    && first.X + first.Width > second.X
    && first.Y < second.Y + second.Height
    && first.Y + first.Height > second.Y

let private contentLayout size =
    let content = gameplayRegionForSize size
    let cell =
        min
            ((content.Bounds.Width - 64.0) / 10.0)
            ((content.Bounds.Height - 48.0) / 20.0)
        |> max 10.0

    let contentWidth = cell * 10.0
    let contentHeight = cell * 20.0
    let contentX = content.Bounds.X + 32.0
    let contentY = content.Bounds.Y + 24.0

    contentX, contentY, cell, contentWidth, contentHeight

let activeGameplayBoundsForSize size model : LayoutGameplayBounds =
    let contentX, contentY, cell, _, _ = contentLayout size
    { Name = "active-item"
      Bounds =
        { X = contentX + float model.ContentColumn * cell + 1.0
          Y = contentY + float model.ContentRow * cell + 1.0
          Width = cell * 2.0 - 2.0
          Height = cell * 2.0 - 2.0 } }

let movementUsesGameplayRegion size model =
    let region = gameplayRegionForSize size
    let bounds = activeGameplayBoundsForSize size model
    boundsInside region.Bounds bounds.Bounds

let spawnUsesGameplayRegion size model =
    let region = gameplayRegionForSize size
    let bounds = activeGameplayBoundsForSize size { model with ContentColumn = 0; ContentRow = 0 }
    boundsInside region.Bounds bounds.Bounds

let collisionUsesGameplayRegion size model =
    movementUsesGameplayRegion size model

let private hudTextBounds (size: Size) model =
    let text width x y name value =
        { Name = name
          Text = value
          Bounds = { X = x; Y = y; Width = width; Height = 24.0 }
          MeasurementMode = ApproximateTextBounds }

    [ text 128.0 16.0 16.0 "items" $"items: {model.ItemCount}"
      text 96.0 168.0 16.0 "step" $"step: {model.Step}"
      text 96.0 296.0 16.0 "next" $"next: {model.NextLabel}"
      text 152.0 (float size.Width - 184.0) 16.0 "status" $"page: {pageName model.Page}" ]

let private overlapDiagnostics (report: LayoutEvidenceReport) =
    let hudTextOverlaps =
        report.TextBounds
        |> List.mapi (fun index first ->
            report.TextBounds
            |> List.skip (index + 1)
            |> List.choose (fun second ->
                if intersects first.Bounds second.Bounds then
                    Some
                        { Kind = HudTextOverlap
                          FirstName = first.Name
                          SecondName = Some second.Name
                          Bounds = first.Bounds
                          Message = $"HUD text '{first.Name}' overlaps '{second.Name}'" }
                else
                    None))
        |> List.concat

    let hudGameplayOverlaps =
        report.TextBounds
        |> List.collect (fun text ->
            report.GameplayBounds
            |> List.choose (fun gameplay ->
                if intersects text.Bounds gameplay.Bounds then
                    Some
                        { Kind = HudGameplayOverlap
                          FirstName = text.Name
                          SecondName = Some gameplay.Name
                          Bounds = text.Bounds
                          Message = $"HUD text '{text.Name}' overlaps gameplay '{gameplay.Name}'" }
                else
                    None))

    hudTextOverlaps @ hudGameplayOverlaps

let layoutEvidenceForSize size model : LayoutEvidenceReport =
    let report =
        { Scene = Scene.empty
          OutputSize = size
          ProofLevel = ReadableLayout
          HudRegion = Some(hudRegionForSize size)
          GameplayRegion = Some(gameplayRegionForSize size)
          TextBounds = hudTextBounds size model
          GameplayBounds = [ activeGameplayBoundsForSize size model ]
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = ApproximateTextBounds
          UnsupportedReasons = []
          Diagnostics = [ "hud-region=present"; "gameplay-region=present"; "measurement-mode=approximate" ]
          RenderEvidence = None }

    let overlaps = overlapDiagnostics report

    if overlaps.IsEmpty then
        report
    else
        { report with
            ProofLevel = DeterministicRenderOnly
            OverlapStatus = LayoutOverlaps overlaps
            Diagnostics = report.Diagnostics @ (overlaps |> List.map _.Message) }

let validateGeneratedLayout (report: LayoutEvidenceReport) =
    let overlaps = overlapDiagnostics report

    let diagnostics =
        [ if report.HudRegion.IsNone then
              "missing HUD region"
          if report.GameplayRegion.IsNone then
              "missing gameplay region"
          if report.TextBounds.IsEmpty then
              "missing HUD text bounds"
          if report.GameplayBounds.IsEmpty then
              "missing gameplay bounds"
          for overlap in overlaps do
              overlap.Message ]

    if diagnostics.IsEmpty then
        { Accepted = true
          FailureClass = None
          Diagnostics = [] }
    else
        { Accepted = false
          FailureClass = if overlaps.IsEmpty then Some MissingLayoutFacts else Some OverlappingLayoutBounds
          Diagnostics = diagnostics }

//#endif
//#endif
