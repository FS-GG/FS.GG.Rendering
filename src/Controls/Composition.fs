namespace FS.GG.UI.Controls

open System
open System.Text
open FS.GG.UI.Scene

module internal Composition =

    type EffectInvalidation =
        { AffectsLayout: bool
          AffectsPaint: bool
          AffectsOrder: bool
          Reason: string }

    type ModifierEffect =
        | Clip of Clip
        | Opacity of float
        | Offset of dx: float * dy: float
        | Transform of PerspectiveTransform
        | Background of Scene
        | Overlay of Scene
        | CacheBoundary of cacheId: uint64
        | LocalZOrder of z: int
        | LayerHint of layerId: string

    type ModifierSource =
        | AuthoredModifier
        | LegacyClipSource
        | LegacyTranslateSource
        | LegacyPerspectiveSource
        | LegacyCacheSource
        | LegacyTextSource
        | LegacyOverlaySource
        | GlyphRunProof

    type ModifierEntry =
        { Effect: ModifierEffect
          Source: ModifierSource }

    type ModifierDiagnostic =
        { Code: string
          Message: string }

    type ModifierChain =
        { Effects: ModifierEntry list
          NormalizedEffects: ModifierEntry list
          FingerprintInput: string
          Diagnostics: ModifierDiagnostic list }

    let private invalidation layout paint order reason =
        { AffectsLayout = layout
          AffectsPaint = paint
          AffectsOrder = order
          Reason = reason }

    let classificationTable =
        [ "clip", invalidation false true false "clip changes repaint the clipped subtree"
          "opacity", invalidation false true false "opacity changes repaint without changing layout"
          "offset", invalidation false true false "offset changes paint placement without remeasuring"
          "transform", invalidation false true false "transform changes paint projection"
          "background", invalidation false true false "background changes repaint the content group"
          "overlay", invalidation false true true "overlay changes repaint and reorder layered content"
          "cache-boundary", invalidation false true false "cache boundary changes repaint/replay identity"
          "local-z-order", invalidation false false true "local z-order changes sibling ordering only"
          "layer-hint", invalidation false false true "layer hints change ordered layer routing only" ]

    let private table name =
        classificationTable
        |> List.find (fun (n, _) -> n = name)
        |> snd

    let classify effect =
        match effect with
        | Clip _ -> table "clip"
        | Opacity _ -> table "opacity"
        | Offset _ -> table "offset"
        | Transform _ -> table "transform"
        | Background _ -> table "background"
        | Overlay _ -> table "overlay"
        | CacheBoundary _ -> table "cache-boundary"
        | LocalZOrder _ -> table "local-z-order"
        | LayerHint _ -> table "layer-hint"

    let private effectName effect =
        match effect with
        | Clip _ -> "clip"
        | Opacity _ -> "opacity"
        | Offset _ -> "offset"
        | Transform _ -> "transform"
        | Background _ -> "background"
        | Overlay _ -> "overlay"
        | CacheBoundary _ -> "cache-boundary"
        | LocalZOrder _ -> "local-z-order"
        | LayerHint _ -> "layer-hint"

    let private isIdentity effect =
        match effect with
        | Opacity value -> value = 1.0
        | Offset(0.0, 0.0) -> true
        | LocalZOrder 0 -> true
        | LayerHint layerId -> String.IsNullOrWhiteSpace layerId || layerId = "content"
        | _ -> false

    let private combine left right =
        match left.Effect, right.Effect with
        | Offset(ax, ay), Offset(bx, by) ->
            Some { right with Effect = Offset(ax + bx, ay + by) }
        | Opacity a, Opacity b ->
            Some { right with Effect = Opacity(a * b) }
        | LocalZOrder _, LocalZOrder _ ->
            Some right
        | LayerHint _, LayerHint _ ->
            Some right
        | CacheBoundary _, CacheBoundary _ ->
            Some right
        | _ -> None

    let private diagnosticFor entry =
        match entry.Effect with
        | Opacity value when value < 0.0 || value > 1.0 ->
            Some
                { Code = "opacity-out-of-range"
                  Message = sprintf "Opacity %.3f is outside the supported 0..1 range." value }
        | CacheBoundary 0UL ->
            Some
                { Code = "cache-boundary-zero"
                  Message = "Cache boundary id 0 is reserved for no cache boundary." }
        | LayerHint layerId when String.IsNullOrWhiteSpace layerId ->
            Some
                { Code = "empty-layer-hint"
                  Message = "Layer hints must name a non-empty target layer." }
        | _ -> None

    let private sceneDigest (scene: Scene) =
        let count = scene.Nodes.Length
        let kinds = Scene.describe scene |> List.map string |> String.concat ","
        sprintf "scene(%d:%s)" count kinds

    let private effectInput effect =
        match effect with
        | Clip clip -> sprintf "clip:%A" clip
        | Opacity value -> sprintf "opacity:%.12g" value
        | Offset(dx, dy) -> sprintf "offset:%.12g,%.12g" dx dy
        | Transform transform -> sprintf "transform:%A" transform
        | Background scene -> sprintf "background:%s" (sceneDigest scene)
        | Overlay scene -> sprintf "overlay:%s" (sceneDigest scene)
        | CacheBoundary cacheId -> sprintf "cache:%d" cacheId
        | LocalZOrder z -> sprintf "z:%d" z
        | LayerHint layerId -> sprintf "layer:%s" layerId

    let private fingerprintInput effects =
        effects
        |> List.map (fun entry -> sprintf "%A:%s" entry.Source (effectInput entry.Effect))
        |> String.concat "|"

    let private fnv1a (text: string) =
        let mutable h = 0xcbf29ce484222325UL // mutable: compact deterministic hash accumulator
        let prime = 0x100000001b3UL
        for b in Encoding.UTF8.GetBytes text do
            h <- (h ^^^ uint64 b) * prime
        h

    let normalize effects =
        let diagnostics = effects |> List.choose diagnosticFor

        let folder acc entry =
            if isIdentity entry.Effect then
                acc
            else
                match acc with
                | previous :: rest ->
                    match combine previous entry with
                    | Some combined when isIdentity combined.Effect -> rest
                    | Some combined -> combined :: rest
                    | None -> entry :: acc
                | [] -> [ entry ]

        let normalized = effects |> List.fold folder [] |> List.rev

        { Effects = effects
          NormalizedEffects = normalized
          FingerprintInput = fingerprintInput normalized
          Diagnostics = diagnostics }

    let fingerprint effects =
        effects |> normalize |> fun chain -> fnv1a chain.FingerprintInput

    let private mapColor opacity color =
        let bounded = Math.Clamp(opacity, 0.0, 1.0)
        { color with Alpha = byte (Math.Round(float color.Alpha * bounded)) }

    let rec private mapSceneNodes opacity nodes =
        nodes
        |> List.map (fun node ->
            match node with
            | Rectangle(bounds, color) -> Rectangle(bounds, mapColor opacity color)
            | PaintedRectangle(bounds, paint) -> PaintedRectangle(bounds, { paint with Opacity = paint.Opacity * opacity })
            | Circle(center, radius, fill) -> Circle(center, radius, mapColor opacity fill)
            | FilledEllipse(bounds, fill) -> FilledEllipse(bounds, mapColor opacity fill)
            | Ellipse(bounds, paint) -> Ellipse(bounds, { paint with Opacity = paint.Opacity * opacity })
            | Line(a, b, paint) -> Line(a, b, { paint with Opacity = paint.Opacity * opacity })
            | Path(path, paint) -> Path(path, { paint with Opacity = paint.Opacity * opacity })
            | Points(points, paint) -> Points(points, { paint with Opacity = paint.Opacity * opacity })
            | Vertices(mode, vertices, paint) -> Vertices(mode, vertices, { paint with Opacity = paint.Opacity * opacity })
            | Arc(bounds, startAngle, sweepAngle, paint) -> Arc(bounds, startAngle, sweepAngle, { paint with Opacity = paint.Opacity * opacity })
            | Text(pos, text, color) -> Text(pos, text, mapColor opacity color)
            | TextRun run -> TextRun { run with Paint = { run.Paint with Opacity = run.Paint.Opacity * opacity } }
            | ClipNode(clip, scene) -> ClipNode(clip, { scene with Nodes = mapSceneNodes opacity scene.Nodes })
            | Group scenes -> Group(scenes |> List.map (fun s -> { s with Nodes = mapSceneNodes opacity s.Nodes }))
            | ColorSpaceNode(cs, scene) -> ColorSpaceNode(cs, { scene with Nodes = mapSceneNodes opacity scene.Nodes })
            | PerspectiveNode(transform, scene) -> PerspectiveNode(transform, { scene with Nodes = mapSceneNodes opacity scene.Nodes })
            | PictureNode picture -> PictureNode { picture with Scene = { picture.Scene with Nodes = mapSceneNodes opacity picture.Scene.Nodes } }
            | Translate(offset, scene) -> Translate(offset, { scene with Nodes = mapSceneNodes opacity scene.Nodes })
            | SizedText(pos, text, size, color) -> SizedText(pos, text, size, mapColor opacity color)
            | CachedSubtree boundary -> CachedSubtree { boundary with Scene = { boundary.Scene with Nodes = mapSceneNodes opacity boundary.Scene.Nodes } }
            | Image _
            | RegionNode _
            | Chart _
            | Empty -> node
            | GlyphRun run -> GlyphRun { run with Paint = { run.Paint with Opacity = run.Paint.Opacity * opacity } })

    let private applyOpacity opacity scenes =
        scenes |> List.map (fun scene -> { scene with Nodes = mapSceneNodes opacity scene.Nodes })

    let applyChain chain content =
        chain.NormalizedEffects
        |> List.fold
            (fun scenes entry ->
                match entry.Effect with
                | Clip clip -> [ Scene.clipped clip (Scene.group scenes) ]
                | Opacity opacity -> applyOpacity opacity scenes
                | Offset(dx, dy) -> [ Scene.translate dx dy (Scene.group scenes) ]
                | Transform transform -> [ Scene.withPerspective transform (Scene.group scenes) ]
                | Background scene -> scene :: scenes
                | Overlay scene -> scenes @ [ scene ]
                | CacheBoundary cacheId ->
                    let wrapped = Scene.group scenes
                    [ { Nodes = [ CachedSubtree { CacheId = cacheId; Fingerprint = fingerprint [ entry ]; Scene = wrapped } ] } ]
                | LocalZOrder _
                | LayerHint _ -> scenes)
            content

    type OrderedContribution =
        { Id: string
          DeclIndex: int
          LocalZ: int
          Layer: string
          Scene: Scene list
          HitBounds: Rect option }

    let contribution id declIndex localZ layer scene hitBounds =
        { Id = id
          DeclIndex = declIndex
          LocalZ = localZ
          Layer = layer
          Scene = scene
          HitBounds = hitBounds }

    let orderSiblings contributions =
        contributions |> List.sortBy (fun c -> c.LocalZ, c.DeclIndex)

    let paintOrder contributions = orderSiblings contributions

    let hitOrder contributions =
        contributions |> paintOrder |> List.rev

    type LayerHost =
        { Id: string
          Order: int
          EscapesClip: bool }

    type Portal =
        { TargetLayer: string
          AnchorId: string option
          AnchorBounds: Rect option
          Content: OrderedContribution }

    type PortalDiagnostic =
        { Code: string
          Message: string
          TargetLayer: string option
          AnchorId: string option }

    type LayerComposition =
        { Paint: OrderedContribution list
          Hit: OrderedContribution list
          Diagnostics: PortalDiagnostic list }

    let layerHost id order escapesClip =
        { Id = id; Order = order; EscapesClip = escapesClip }

    let portal targetLayer anchorId anchorBounds content =
        { TargetLayer = targetLayer
          AnchorId = anchorId
          AnchorBounds = anchorBounds
          Content = content }

    let composeLayers (hosts: LayerHost list) (inFlow: OrderedContribution list) (portals: Portal list) =
        let contentHost = layerHost "content" 0 false
        let allHosts: LayerHost list =
            contentHost :: hosts
            |> List.groupBy _.Id
            |> List.map (fun (_, values) -> values |> List.sortBy _.Order |> List.head)
            |> List.sortBy _.Order

        let hostIds: Set<string> = allHosts |> List.map _.Id |> Set.ofList

        let diagnostics =
            portals
            |> List.collect (fun (p: Portal) ->
                [ if not (Set.contains p.TargetLayer hostIds) then
                      { Code = "missing-portal-target"
                        Message = sprintf "Portal target layer '%s' is not registered." p.TargetLayer
                        TargetLayer = Some p.TargetLayer
                        AnchorId = p.AnchorId }
                  if Option.isNone p.AnchorId || Option.isNone p.AnchorBounds then
                      { Code = "missing-portal-anchor"
                        Message = sprintf "Portal targeting '%s' lacks anchor identity or bounds evidence." p.TargetLayer
                        TargetLayer = Some p.TargetLayer
                        AnchorId = p.AnchorId } ])

        let portalContributions =
            portals
            |> List.filter (fun (p: Portal) -> Set.contains p.TargetLayer hostIds && Option.isSome p.AnchorId && Option.isSome p.AnchorBounds)
            |> List.map (fun (p: Portal) -> { p.Content with Layer = p.TargetLayer })

        let contributions =
            inFlow @ portalContributions
            |> List.groupBy _.Layer
            |> Map.ofList

        let layerPaint =
            allHosts
            |> List.collect (fun host ->
                contributions
                |> Map.tryFind host.Id
                |> Option.defaultValue []
                |> paintOrder)

        let layerHit =
            allHosts
            |> List.rev
            |> List.collect (fun host ->
                contributions
                |> Map.tryFind host.Id
                |> Option.defaultValue []
                |> hitOrder)

        { Paint = layerPaint
          Hit = layerHit
          Diagnostics = diagnostics }

    type LegacyForm =
        | LegacyClipping of Clip
        | LegacyTranslation of dx: float * dy: float
        | LegacyPerspective of PerspectiveTransform
        | LegacyCachedSubtree of cacheId: uint64
        | LegacyText
        | LegacyOverlay

    type LegacyCompatibilityStatus =
        | SupportedUnchanged
        | DeprecatedWithMigration of note: string
        | IntentionallyChanged of note: string

    let legacyLower form =
        let entry source effect = { Source = source; Effect = effect }

        match form with
        | LegacyClipping clip -> [ entry LegacyClipSource (Clip clip) ]
        | LegacyTranslation(dx, dy) -> [ entry LegacyTranslateSource (Offset(dx, dy)) ]
        | LegacyPerspective transform -> [ entry LegacyPerspectiveSource (Transform transform) ]
        | LegacyCachedSubtree cacheId -> [ entry LegacyCacheSource (CacheBoundary cacheId) ]
        | LegacyText -> [ entry LegacyTextSource (LayerHint "content") ]
        | LegacyOverlay -> [ entry LegacyOverlaySource (LayerHint "overlay") ]

    let compatibilityEvidence form =
        match form with
        | LegacyClipping _ -> SupportedUnchanged, "Legacy ClipNode lowers to a clip modifier and preserves clipped output."
        | LegacyTranslation _ -> SupportedUnchanged, "Legacy Translate lowers to an offset modifier and preserves paint placement."
        | LegacyPerspective _ -> SupportedUnchanged, "Legacy PerspectiveNode lowers to a transform modifier and preserves projection."
        | LegacyCachedSubtree _ -> SupportedUnchanged, "Legacy CachedSubtree lowers to a cache-boundary modifier with deterministic fingerprint input."
        | LegacyText -> SupportedUnchanged, "Legacy text and text-run nodes remain Scene text forms unless glyph-run proof is explicitly authored."
        | LegacyOverlay -> SupportedUnchanged, "Legacy Overlay control output is modeled as portal/layer routing while preserving z-top paint order."
