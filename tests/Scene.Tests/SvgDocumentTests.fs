module SvgDocumentTests

open System
open Expecto
open FS.GG.UI.Scene
open PortableDocumentFixture

let private point x y = { X = x; Y = y }
let private rect x y width height = { X = x; Y = y; Width = width; Height = height }

let private leaf id =
    { Id = id
      SemanticId = Some("semantic:" + id)
      Visible = true
      Transform = SvgAffine.identity
      ClipId = None
      MaskId = None
      Presentation = Some SvgDocument.defaultPresentation
      Content = SvgElementContent.SceneLeaf(Scene.rectangle (0.0, 0.0, 1.0, 1.0) Colors.black) }

let private document definitions children =
    { Schema = SvgDocument.schema
      Id = "fixture"
      ViewBox = rect 0.0 0.0 100.0 100.0
      Definitions = definitions
      Children = children }

[<Tests>]
let tests =
    testList "SVG document and affine contract" [
        test "composition applies local before parent using independent arithmetic" {
            let parent = SvgAffine.translate 10.0 20.0
            let local = SvgAffine.rotateDegrees 90.0
            let actual = SvgAffine.transformPoint (SvgAffine.compose parent local) (point 2.0 3.0)
            Expect.floatClose Accuracy.high actual.X 7.0 "rotated x then translated"
            Expect.floatClose Accuracy.high actual.Y 22.0 "rotated y then translated"

            let reflectedSkew = SvgAffine.compose (SvgAffine.scale -1.0 2.0) (SvgAffine.skewXDegrees 45.0)
            let reflected = SvgAffine.transformPoint reflectedSkew (point 2.0 3.0)
            Expect.floatClose Accuracy.medium reflected.X -5.0 "skew then reflection"
            Expect.floatClose Accuracy.medium reflected.Y 6.0 "skew then nonuniform scale"
        }

        test "inverse distinguishes finite singular and non-finite matrices" {
            let transform = SvgAffine.compose (SvgAffine.translate 8.0 -4.0) (SvgAffine.scale -2.0 3.0)
            let source = point 5.0 7.0
            let projected = SvgAffine.transformPoint transform source
            match SvgAffine.tryInverse transform with
            | Ok inverse ->
                let restored = SvgAffine.transformPoint inverse projected
                Expect.floatClose Accuracy.high restored.X source.X "inverse restores x"
                Expect.floatClose Accuracy.high restored.Y source.Y "inverse restores y"
            | Error error -> failtestf "unexpected inverse error %A" error
            Expect.equal (SvgAffine.tryInverse (SvgAffine.scale 0.0 1.0)) (Error SvgAffineError.Singular) "singular is explicit"
            Expect.equal (SvgAffine.tryInverse { SvgAffine.identity with A = Double.NaN }) (Error SvgAffineError.NonFinite) "non-finite is explicit"
        }

        test "valid identified document resolves local definitions" {
            let symbol = { Id = "symbol-a"; Content = SvgDefinitionContent.Symbol(Some(rect 0.0 0.0 1.0 1.0), [ leaf "symbol-child" ]) }
            let instance = { leaf "instance-a" with Content = SvgElementContent.SymbolInstance("symbol-a", Some(rect 0.0 0.0 5.0 5.0)) }
            let issues = SvgDocument.validate 512 SvgDocument.defaultLimits (document [ symbol ] [ instance ])
            Expect.isEmpty issues "valid references and bounds pass"
        }

        test "duplicates missing references cycles limits and non-finite transforms fail loud" {
            let cyclicChild = { leaf "cycle-child" with Content = SvgElementContent.SymbolInstance("cycle-a", None) }
            let cyclic = { Id = "cycle-a"; Content = SvgDefinitionContent.Symbol(None, [ cyclicChild ]) }
            let invalid =
                { leaf "duplicate" with
                    Transform = { SvgAffine.identity with E = Double.PositiveInfinity }
                    ClipId = Some "missing" }
            let constrained = { SvgDocument.defaultLimits with MaxNodes = 1; MaxDefinitions = 0; MaxSerializedBytes = 1 }
            let issues = SvgDocument.validate 2 constrained (document [ cyclic ] [ invalid; leaf "duplicate" ])
            let codes = issues |> List.map _.Code |> Set.ofList
            [ "duplicate-id"; "missing-reference"; "cyclic-reference"; "non-finite-transform"; "node-limit"; "definition-limit"; "document-byte-limit" ]
            |> List.iter (fun code -> Expect.contains codes code $"{code} is diagnosed")

            let rootCollision = SvgDocument.validate 0 SvgDocument.defaultLimits { document [] [ leaf "fixture" ] with Id = "fixture" }
            Expect.exists rootCollision (fun value -> value.Code = "duplicate-id") "document root identity shares the global namespace"
        }

        test "definition kinds and direct Scene leaves cannot bypass validation" {
            let emptyClip = { Id = "clip"; Content = SvgDefinitionContent.Clip(SvgCoordinateUnits.UserSpaceOnUse, []) }
            let emptyMask = { Id = "mask"; Content = SvgDefinitionContent.Mask(SvgCoordinateUnits.UserSpaceOnUse, rect 0.0 0.0 1.0 1.0, SvgMaskKind.Alpha, []) }
            let emptySymbol = { Id = "symbol"; Content = SvgDefinitionContent.Symbol(None, []) }
            let gradient =
                { Id = "gradient"
                  Content =
                    SvgDefinitionContent.Gradient
                        { Geometry = SvgGradientGeometry.Linear(point 0.0 0.0, point 1.0 1.0)
                          Units = SvgCoordinateUnits.ObjectBoundingBox
                          Transform = SvgAffine.identity
                          Spread = SvgSpreadMethod.Pad
                          Stops = []
                          InheritFrom = Some "symbol" } }
            let crossed =
                { leaf "crossed" with
                    ClipId = Some "mask"
                    MaskId = Some "clip"
                    Presentation = Some { SvgDocument.defaultPresentation with FillSource = Some(SvgPaintSource.Definition "symbol") }
                    Content = SvgElementContent.SymbolInstance("gradient", None) }
            let unsupported =
                { leaf "unsupported" with
                    Content = SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Image((0.0, 0.0, 1.0, 1.0), "external.png") ] } }
            let issues = SvgDocument.validate 100 SvgDocument.defaultLimits (document [ emptyClip; emptyMask; emptySymbol; gradient ] [ crossed; unsupported ])
            Expect.equal (issues |> List.filter (fun value -> value.Code = "wrong-reference-kind") |> List.length) 5 "every typed reference refuses the wrong definition kind"
            Expect.exists issues (fun value -> value.Code = "unsupported-scene-leaf") "direct construction cannot bypass Scene support checks"
        }

        test "invalid symbol viewport does not bypass reference validation" {
            let instance =
                { leaf "invalid-instance" with
                    Content = SvgElementContent.SymbolInstance("missing-symbol", Some(rect 0.0 0.0 Double.NaN 1.0)) }
            let issues = SvgDocument.validate 100 SvgDocument.defaultLimits (document [] [ instance ])
            let codes = issues |> List.map _.Code |> Set.ofList
            Expect.contains codes "invalid-symbol-viewport" "invalid viewport is diagnosed"
            Expect.contains codes "missing-reference" "the symbol reference is still validated"
        }

        test "foundation adapter preserves semantic and render identity separately" {
            let retained =
                { RootId = "root"
                  Revision = 1
                  Camera = { PanX = 0.0; PanY = 0.0; Zoom = 1.0 }
                  Layers =
                    [ { Id = "world"
                        Visible = true
                        Objects =
                          [ { Id = "unit-7"
                              Selectable = true
                              AccessibleLabel = "unit seven"
                              Content = Scene.circle (point 2.0 3.0) 1.0 Colors.black } ] } ] }
            match SvgDocument.ofRetainedScene (rect 0.0 0.0 10.0 10.0) retained with
            | Error issues -> failtestf "adapter failed: %A" issues
            | Ok accepted ->
                match accepted.Children with
                | [ { Id = "layer:world"; Content = SvgElementContent.Group [ child ] } ] ->
                    Expect.equal child.Id "object:unit-7" "render-node identity is namespaced"
                    Expect.equal child.SemanticId (Some "unit-7") "semantic identity is preserved separately"
                    Expect.isTrue child.Visible "object remains visible"
                | children -> failtestf "unexpected adapter tree %A" children

            let hidden =
                { retained with
                    Layers = retained.Layers |> List.map (fun layer -> { layer with Visible = false }) }
            match SvgDocument.ofRetainedScene (rect 0.0 0.0 10.0 10.0) hidden with
            | Ok { Children = [ layer ] } -> Expect.isFalse layer.Visible "layer visibility is preserved"
            | result -> failtestf "unexpected hidden adapter result %A" result
        }

        test "selected document surface round trips canonically on .NET" {
            let serialized, exported = verifyRoundTrip "scene-tests"
            match SvgDocument.deserialize serialized with
            | Ok restored -> Expect.equal (SvgDocument.serialize restored) (Ok serialized) "the typed document canonical round trip is exact"
            | Error issues -> failtestf "round trip failed: %A" issues
            Expect.stringContains exported "fill=\"none\" stroke=\"rgb(240 120 20)\"" "stroke style has no interior and SolidColor overrides Fill"
            Expect.stringContains exported "fill-rule=\"evenodd\"" "evenodd holes are preserved"
            Expect.stringContains exported "spreadMethod=\"reflect\"" "gradient spread is preserved"
            Expect.stringContains exported "gradientTransform=\"matrix(" "gradient transform is preserved"
            Expect.stringContains exported "color-interpolation=\"sRGB\"" "selected gradients declare sRGB interpolation"
            Expect.stringContains exported "text-anchor=\"start\" direction=\"auto\"" "existing text semantics are explicit in standalone export"
            Expect.stringContains exported "mask-type:alpha" "alpha masks are explicit"
            Expect.stringContains exported "mask-type:luminance" "luminance masks are explicit"
            Expect.stringContains exported "<symbol" "symbol definitions export"
            Expect.stringContains exported "<use" "symbol instances export"
            let arcSegments = exported.Split(" A ").Length - 1
            Expect.isGreaterThanOrEqual arcSegments 2 "complete revolutions split into multiple SVG arc segments"
        }

        test "typed codec refuses XML and unsupported content with stable location" {
            match SvgDocument.deserialize "<svg/>" with
            | Error [ issue ] ->
                Expect.equal issue.Code "invalid-serialization" "arbitrary SVG XML is not treated as the typed format"
                Expect.equal issue.Location "/" "codec diagnostics have a stable root location"
            | result -> failtestf "unexpected arbitrary import result: %A" result

            let unsupported =
                { PortableDocumentFixture.document with
                    Children =
                        [ { leaf "bad-node" with
                              Content = SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Image((0.0, 0.0, 2.0, 2.0), "remote.png") ] } } ] }
            match SvgDocument.exportSvg "test" unsupported with
            | Error issues ->
                Expect.exists issues (fun issue -> issue.Code = "unsupported-scene-leaf" && issue.Location = "/children/0/scene/nodes/0") "unsupported object/node fails before export at a stable path"
            | Ok _ -> failtest "unsupported image unexpectedly exported"
        }

        test "malformed geometry and unsafe local font declarations fail before export" {
            let malformed =
                { PortableDocumentFixture.document with
                    Definitions =
                        PortableDocumentFixture.document.Definitions
                        |> List.map (fun definition ->
                            match definition.Content with
                            | SvgDefinitionContent.Font font ->
                                { definition with Content = SvgDefinitionContent.Font { font with Family = "unsafe\";src:url(remote)" } }
                            | _ -> definition)
                    Children =
                        [ { leaf "negative-rect" with
                              Content = SvgElementContent.SceneLeaf { Nodes = [ SceneNode.Rectangle((0.0, 0.0, -1.0, 2.0), Colors.black) ] } } ] }
            match SvgDocument.exportSvg "test" malformed with
            | Error issues ->
                Expect.exists issues (fun issue -> issue.Code = "invalid-font-family" && issue.Location.EndsWith("/family")) "CSS-significant font family characters are refused"
                Expect.exists issues (fun issue -> issue.Code = "unsupported-scene-leaf" && issue.Location = "/children/0/scene/nodes/0") "negative SVG rectangle geometry has a stable node location"
            | Ok _ -> failtest "malformed document unexpectedly exported"
        }
    ]
