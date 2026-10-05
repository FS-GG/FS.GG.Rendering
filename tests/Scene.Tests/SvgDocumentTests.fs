module SvgDocumentTests

open System
open System.Xml.Linq
open Expecto
open FS.GG.UI.Scene
open PortableDocumentFixture

let private point x y = { X = x; Y = y }

let private rect x y width height =
    {
        X = x
        Y = y
        Width = width
        Height = height
    }

let private leaf id =
    {
        Id = id
        SemanticId = Some("semantic:" + id)
        Visible = true
        Transform = SvgAffine.identity
        ClipId = None
        MaskId = None
        Presentation = Some SvgDocument.defaultPresentation
        Content = SvgElementContent.SceneLeaf(Scene.rectangle (0.0, 0.0, 1.0, 1.0) Colors.black)
    }

let private document definitions children =
    {
        Schema = SvgDocument.schema
        Id = "fixture"
        ViewBox = rect 0.0 0.0 100.0 100.0
        Definitions = definitions
        Children = children
    }

// Frozen by actual unchanged-producer .NET 10.0.12 execution before factoring.
// The independent Fable baseline is checked separately by the browser consumer.
let private prefixPrechangeDigests =
    [
        "gallery", "91948c47a8245b063c8a32c5861ece90c71446d8a41bbf63f98bd748b21dcdca", "6a4591362d98ea820035c6e004e258482f90af5fe8061b595a8e3876490fae38"
        "ascii-id", "eda3bff9f260d3396de1a5baa4493ed372393ee922937cd92c612386748422a7", "d930505e5ba566bafffdab4a64446d005cb0541f55fd059505613d8d9e145a23"
        "escaped-punctuation", "9cb301ec1730ce8ef92308fdf74c32969b659f98ef04875bcf633695c6116951", "5ad13ffcbea0e7a08786d3279a7486a57bf0ed252bdb7aafff415134c31a50c4"
        "unicode-id", "8a54a31ad81dcfbcfbe272d99eec6c9a5231bcf4f0574e51276d8b13283aeb1c", "0d76f8155764f52f503084966a826680763e83bf46d5c520d799b71441dad316"
        "namespace-a", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
        "namespace-b", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "2aaa6b2549aca1df34d6803c84d085d370b2cb25f4b6510892c188628f54c007"
        "document-a", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "f4596bbbea6dea2f8119bdb27ded54061815bd02c0a0f4e2e69673930c077316"
        "document-b", "fd000a0b56e81317b70a7cf0ee69314286dfc139ff227357d84621bae9d3d755", "1a536cc4b6a296a935903fe24f0bbeeba171712eea717357043f5f3112dcba70"
        "definition-replaced", "e9dd57d245ae1dbef4ce2985396347caf1534f22785c01c5838ff1d9c4fabce8", "c1015202ee14ec42ab139574dedc998d6843abb65d1e93be0aacea94aaf096e8"
        "reference-changed", "21352ccadc2e1aeff6b6c42f232240813f6964f423f22d6b8ccb44f34a898a15", "9f26771da92ac20910ceaa997c0fadfe070bff1e12fe3dce51d638518e82de32"
        "reference-removed", "aed6e9dc17e20b85b66bdf9d57bba66963bdb25cb1269c36515d2bba30d1dd96", "c90d6a62ffe352ef6de3f99b521bb93134bc4493a641a95c9316282af2c28983"
        "interleave-a-first", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
        "interleave-b", "fd000a0b56e81317b70a7cf0ee69314286dfc139ff227357d84621bae9d3d755", "c7ba03084d0481d6c4b5deaeddb6757cb51a59c47c33145c1b8ebdacbf842697"
        "interleave-a-again", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "d61b5fc6d7382605de7645f896403d9ef722aadab848f73d0fec3335fb49ddaf"
        "gallery-second-document", "6e64ad706729cc9267521117178cf0d537408eee4f1b555a5792a895ca32790c", "e77d863b711994e34b27de2a117aa677be7ea1fa24557dff91b9dfbe0cf68619"
        "invalid-blank-namespace", "2c55ca1a8ae84c5413e76adef809fc9dddb711d4db4653b98b2a95f983dc1bb4", "c0259da567fde755eb0feea735f352a2be968958ccf459046edc0a05407196a5"
        "invalid-blank-document", "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b", "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b"
        "invalid-both-blank", "5ee49b9eaef4ba18b89439ead8dee9372ed3932660eb13933957172853044e8b", "2e310bd2f1278c399fedcbc7564add9289fe61ee85cc0023bb2346d475aed04d"
        "invalid-duplicate", "2a81c7c1c473839bc711ba3210901380e276badd021262e6012cd3d0e04772c3", "2a81c7c1c473839bc711ba3210901380e276badd021262e6012cd3d0e04772c3"
        "invalid-missing-reference", "99a43d5380e50d7fef98c891f4128977db792cb6552ec4b2947812f068fb6e69", "99a43d5380e50d7fef98c891f4128977db792cb6552ec4b2947812f068fb6e69"
        "invalid-wrong-reference-kind", "65b240eefeabd3b0e103c755697d18f54e921f5f4eee4aa875597b8404f812a6", "65b240eefeabd3b0e103c755697d18f54e921f5f4eee4aa875597b8404f812a6"
        "invalid-cycle", "a997cb82763cea0f440259295f9a357ec24ed70d80c65b8407cd08a7ce27cf31", "a997cb82763cea0f440259295f9a357ec24ed70d80c65b8407cd08a7ce27cf31"
    ]

let private prefixDigest (value: string) =
    value
    |> Text.Encoding.UTF8.GetBytes
    |> Security.Cryptography.SHA256.HashData
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

[<Tests>]
let tests =
    testList
        "SVG document and affine contract"
        [
            test "export prefix bytes match the frozen pre-change .NET producer" {
                let actual = prefixCompatibilityObservations ()
                Expect.equal actual.Length prefixPrechangeDigests.Length "every fixed success and refusal is checked"
                for ((name, serialized, svg), (expectedName, serializedDigest, svgDigest)) in List.zip actual prefixPrechangeDigests do
                    Expect.equal name expectedName "fixed corpus order"
                    Expect.equal (prefixDigest serialized) serializedDigest (name + " complete serialized bytes")
                    Expect.equal (prefixDigest svg) svgDigest (name + " complete export or ordered issue bytes")
            }

            test "export prefix corpus captures pre-change runtime bytes" {
                let observations = prefixCompatibilityObservations ()
                Expect.equal observations.Length 22 "fixed successful and refusal corpus population"
                let ascii = observations |> List.find (fun (name, _, _) -> name = "ascii-id") |> fun (_, _, svg) -> svg
                Expect.stringContains ascii "id=\"fsgg-006d-0064-0073\"" "independent m/d/s lowercase hexadecimal ID"
                let interleaved name =
                    observations |> List.find (fun (label, _, _) -> label = name) |> fun (_, _, svg) -> svg
                Expect.equal (interleaved "interleave-a-first") (interleaved "interleave-a-again") "interleaved exports retain call-local scope"

                // This opt-in capture is used once on the unchanged producer, before
                // candidate checks bind immutable runtime-specific byte expectations.
                let output = Environment.GetEnvironmentVariable "FSGG_SVG_PREFIX_CAPTURE" |> Option.ofObj
                match output with
                | Some output when not (String.IsNullOrWhiteSpace output) ->
                    Expect.isTrue (IO.Path.IsPathRooted output) "capture path is explicit and absolute"
                    Expect.isFalse (IO.File.Exists output) "pre-change capture is once-only"
                    let encoded (value: string) = Convert.ToBase64String(Text.Encoding.UTF8.GetBytes value)
                    let text =
                        observations
                        |> List.map (fun (name, serialized, svg) -> $"{name}\t{encoded serialized}\t{encoded svg}")
                        |> String.concat "\n"
                    IO.File.WriteAllText(output, text + "\n", Text.UTF8Encoding(false))
                | _ -> ()
            }

            test "composition applies local before parent using independent arithmetic" {
                let parent = SvgAffine.translate 10.0 20.0
                let local = SvgAffine.rotateDegrees 90.0

                let actual =
                    SvgAffine.transformPoint (SvgAffine.compose parent local) (point 2.0 3.0)

                Expect.floatClose Accuracy.high actual.X 7.0 "rotated x then translated"
                Expect.floatClose Accuracy.high actual.Y 22.0 "rotated y then translated"

                let reflectedSkew =
                    SvgAffine.compose (SvgAffine.scale -1.0 2.0) (SvgAffine.skewXDegrees 45.0)

                let reflected = SvgAffine.transformPoint reflectedSkew (point 2.0 3.0)
                Expect.floatClose Accuracy.medium reflected.X -5.0 "skew then reflection"
                Expect.floatClose Accuracy.medium reflected.Y 6.0 "skew then nonuniform scale"
            }

            test "inverse distinguishes finite singular and non-finite matrices" {
                let transform =
                    SvgAffine.compose (SvgAffine.translate 8.0 -4.0) (SvgAffine.scale -2.0 3.0)

                let source = point 5.0 7.0
                let projected = SvgAffine.transformPoint transform source

                match SvgAffine.tryInverse transform with
                | Ok inverse ->
                    let restored = SvgAffine.transformPoint inverse projected
                    Expect.floatClose Accuracy.high restored.X source.X "inverse restores x"
                    Expect.floatClose Accuracy.high restored.Y source.Y "inverse restores y"
                | Error error -> failtestf "unexpected inverse error %A" error

                Expect.equal
                    (SvgAffine.tryInverse (SvgAffine.scale 0.0 1.0))
                    (Error SvgAffineError.Singular)
                    "singular is explicit"

                Expect.equal
                    (SvgAffine.tryInverse
                        { SvgAffine.identity with
                            A = Double.NaN
                        })
                    (Error SvgAffineError.NonFinite)
                    "non-finite is explicit"
            }

            test "valid identified document resolves local definitions" {
                let symbol =
                    {
                        Id = "symbol-a"
                        Content = SvgDefinitionContent.Symbol(Some(rect 0.0 0.0 1.0 1.0), [ leaf "symbol-child" ])
                    }

                let instance =
                    { leaf "instance-a" with
                        Content = SvgElementContent.SymbolInstance("symbol-a", Some(rect 0.0 0.0 5.0 5.0))
                    }

                let issues =
                    SvgDocument.validate 512 SvgDocument.defaultLimits (document [ symbol ] [ instance ])

                Expect.isEmpty issues "valid references and bounds pass"
            }

            test "duplicates missing references cycles limits and non-finite transforms fail loud" {
                let cyclicChild =
                    { leaf "cycle-child" with
                        Content = SvgElementContent.SymbolInstance("cycle-a", None)
                    }

                let cyclic =
                    {
                        Id = "cycle-a"
                        Content = SvgDefinitionContent.Symbol(None, [ cyclicChild ])
                    }

                let invalid =
                    { leaf "duplicate" with
                        Transform =
                            { SvgAffine.identity with
                                E = Double.PositiveInfinity
                            }
                        ClipId = Some "missing"
                    }

                let constrained =
                    { SvgDocument.defaultLimits with
                        MaxNodes = 1
                        MaxDefinitions = 0
                        MaxSerializedBytes = 1
                    }

                let issues =
                    SvgDocument.validate 2 constrained (document [ cyclic ] [ invalid; leaf "duplicate" ])

                let codes = issues |> List.map _.Code |> Set.ofList

                [
                    "duplicate-id"
                    "missing-reference"
                    "cyclic-reference"
                    "non-finite-transform"
                    "node-limit"
                    "definition-limit"
                    "document-byte-limit"
                ]
                |> List.iter (fun code -> Expect.contains codes code $"{code} is diagnosed")

                let rootCollision =
                    SvgDocument.validate
                        0
                        SvgDocument.defaultLimits
                        { document [] [ leaf "fixture" ] with
                            Id = "fixture"
                        }

                Expect.exists
                    rootCollision
                    (fun value -> value.Code = "duplicate-id")
                    "document root identity shares the global namespace"
            }

            test "definition kinds and direct Scene leaves cannot bypass validation" {
                let emptyClip =
                    {
                        Id = "clip"
                        Content = SvgDefinitionContent.Clip(SvgCoordinateUnits.UserSpaceOnUse, [])
                    }

                let emptyMask =
                    {
                        Id = "mask"
                        Content =
                            SvgDefinitionContent.Mask(
                                SvgCoordinateUnits.UserSpaceOnUse,
                                rect 0.0 0.0 1.0 1.0,
                                SvgMaskKind.Alpha,
                                []
                            )
                    }

                let emptySymbol =
                    {
                        Id = "symbol"
                        Content = SvgDefinitionContent.Symbol(None, [])
                    }

                let gradient =
                    {
                        Id = "gradient"
                        Content =
                            SvgDefinitionContent.Gradient
                                {
                                    Geometry = SvgGradientGeometry.Linear(point 0.0 0.0, point 1.0 1.0)
                                    Units = SvgCoordinateUnits.ObjectBoundingBox
                                    Transform = SvgAffine.identity
                                    Spread = SvgSpreadMethod.Pad
                                    Stops = []
                                    InheritFrom = Some "symbol"
                                }
                    }

                let crossed =
                    { leaf "crossed" with
                        ClipId = Some "mask"
                        MaskId = Some "clip"
                        Presentation =
                            Some
                                { SvgDocument.defaultPresentation with
                                    FillSource = Some(SvgPaintSource.Definition "symbol")
                                }
                        Content = SvgElementContent.SymbolInstance("gradient", None)
                    }

                let unsupported =
                    { leaf "unsupported" with
                        Content =
                            SvgElementContent.SceneLeaf
                                {
                                    Nodes = [ SceneNode.Image((0.0, 0.0, 1.0, 1.0), "external.png") ]
                                }
                    }

                let issues =
                    SvgDocument.validate
                        100
                        SvgDocument.defaultLimits
                        (document [ emptyClip; emptyMask; emptySymbol; gradient ] [ crossed; unsupported ])

                Expect.equal
                    (issues
                     |> List.filter (fun value -> value.Code = "wrong-reference-kind")
                     |> List.length)
                    5
                    "every typed reference refuses the wrong definition kind"

                Expect.exists
                    issues
                    (fun value -> value.Code = "unsupported-scene-leaf")
                    "direct construction cannot bypass Scene support checks"
            }

            test "invalid symbol viewport does not bypass reference validation" {
                let instance =
                    { leaf "invalid-instance" with
                        Content = SvgElementContent.SymbolInstance("missing-symbol", Some(rect 0.0 0.0 Double.NaN 1.0))
                    }

                let issues =
                    SvgDocument.validate 100 SvgDocument.defaultLimits (document [] [ instance ])

                let codes = issues |> List.map _.Code |> Set.ofList
                Expect.contains codes "invalid-symbol-viewport" "invalid viewport is diagnosed"
                Expect.contains codes "missing-reference" "the symbol reference is still validated"
            }

            test "foundation adapter preserves semantic and render identity separately" {
                let retained =
                    {
                        RootId = "root"
                        Revision = 1
                        Camera = { PanX = 0.0; PanY = 0.0; Zoom = 1.0 }
                        Layers =
                            [
                                {
                                    Id = "world"
                                    Visible = true
                                    Objects =
                                        [
                                            {
                                                Id = "unit-7"
                                                Selectable = true
                                                AccessibleLabel = "unit seven"
                                                Content = Scene.circle (point 2.0 3.0) 1.0 Colors.black
                                            }
                                        ]
                                }
                            ]
                    }

                match SvgDocument.ofRetainedScene (rect 0.0 0.0 10.0 10.0) retained with
                | Error issues -> failtestf "adapter failed: %A" issues
                | Ok accepted ->
                    match accepted.Children with
                    | [ {
                            Id = "layer:world"
                            Content = SvgElementContent.Group [ child ]
                        } ] ->
                        Expect.equal child.Id "object:unit-7" "render-node identity is namespaced"
                        Expect.equal child.SemanticId (Some "unit-7") "semantic identity is preserved separately"
                        Expect.isTrue child.Visible "object remains visible"
                    | children -> failtestf "unexpected adapter tree %A" children

                let hidden =
                    { retained with
                        Layers = retained.Layers |> List.map (fun layer -> { layer with Visible = false })
                    }

                match SvgDocument.ofRetainedScene (rect 0.0 0.0 10.0 10.0) hidden with
                | Ok { Children = [ layer ] } -> Expect.isFalse layer.Visible "layer visibility is preserved"
                | result -> failtestf "unexpected hidden adapter result %A" result
            }

            test "selected document surface round trips canonically on .NET" {
                let serialized, exported = verifyRoundTrip "scene-tests"

                match SvgDocument.deserialize serialized with
                | Ok restored ->
                    Expect.equal
                        (SvgDocument.serialize restored)
                        (Ok serialized)
                        "the typed document canonical round trip is exact"
                | Error issues -> failtestf "round trip failed: %A" issues

                Expect.stringContains
                    exported
                    "fill=\"none\" stroke=\"rgb(240 120 20)\""
                    "stroke style has no interior and SolidColor overrides Fill"

                Expect.stringContains exported "fill-rule=\"evenodd\"" "evenodd holes are preserved"
                Expect.stringContains exported "spreadMethod=\"reflect\"" "gradient spread is preserved"
                Expect.stringContains exported "gradientTransform=\"matrix(" "gradient transform is preserved"

                Expect.stringContains
                    exported
                    "color-interpolation=\"sRGB\""
                    "selected gradients declare sRGB interpolation"

                Expect.stringContains
                    exported
                    "text-anchor=\"start\" direction=\"auto\""
                    "existing text semantics are explicit in standalone export"

                Expect.stringContains exported "mask-type:alpha" "alpha masks are explicit"
                Expect.stringContains exported "mask-type:luminance" "luminance masks are explicit"
                Expect.stringContains exported "<symbol" "symbol definitions export"
                Expect.stringContains exported "<use" "symbol instances export"

                Expect.stringContains
                    exported
                    "data-fsgg-symbol-hit=\"symbol-instance\""
                    "symbol viewport exports the cross-browser hit proxy"

                Expect.stringContains
                    exported
                    "fill=\"transparent\" stroke=\"none\" pointer-events=\"all\" aria-hidden=\"true\""
                    "the hit proxy is paintless, targetable, and excluded from accessibility"

                let parsed = XDocument.Parse(exported)

                Expect.equal
                    (parsed.Root |> Option.ofObj |> Option.map (fun root -> root.Name.LocalName))
                    (Some "svg")
                    "standalone export is well-formed SVG/XML"

                let arcSegments = exported.Split(" A ").Length - 1
                Expect.isGreaterThanOrEqual arcSegments 2 "complete revolutions split into multiple SVG arc segments"
            }

            test "typed codec refuses XML and unsupported content with stable location" {
                match SvgDocument.deserialize "<svg/>" with
                | Error [ issue ] ->
                    Expect.equal
                        issue.Code
                        "invalid-serialization"
                        "arbitrary SVG XML is not treated as the typed format"

                    Expect.equal issue.Location "/" "codec diagnostics have a stable root location"
                | result -> failtestf "unexpected arbitrary import result: %A" result

                let unsupported =
                    { PortableDocumentFixture.document with
                        Children =
                            [
                                { leaf "bad-node" with
                                    Content =
                                        SvgElementContent.SceneLeaf
                                            {
                                                Nodes = [ SceneNode.Image((0.0, 0.0, 2.0, 2.0), "remote.png") ]
                                            }
                                }
                            ]
                    }

                match SvgDocument.exportSvg "test" unsupported with
                | Error issues ->
                    Expect.exists
                        issues
                        (fun issue ->
                            issue.Code = "unsupported-scene-leaf"
                            && issue.Location = "/children/0/scene/nodes/0")
                        "unsupported object/node fails before export at a stable path"
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
                                    { definition with
                                        Content =
                                            SvgDefinitionContent.Font
                                                { font with
                                                    Family = "unsafe\";src:url(remote)"
                                                }
                                    }
                                | _ -> definition)
                        Children =
                            [
                                { leaf "negative-rect" with
                                    Content =
                                        SvgElementContent.SceneLeaf
                                            {
                                                Nodes = [ SceneNode.Rectangle((0.0, 0.0, -1.0, 2.0), Colors.black) ]
                                            }
                                }
                            ]
                    }

                match SvgDocument.exportSvg "test" malformed with
                | Error issues ->
                    Expect.exists
                        issues
                        (fun issue -> issue.Code = "invalid-font-family" && issue.Location.EndsWith("/family"))
                        "CSS-significant font family characters are refused"

                    Expect.exists
                        issues
                        (fun issue ->
                            issue.Code = "unsupported-scene-leaf"
                            && issue.Location = "/children/0/scene/nodes/0")
                        "negative SVG rectangle geometry has a stable node location"
                | Ok _ -> failtest "malformed document unexpectedly exported"
            }

            test "minimal document reducer rejects atomically and preserves semantic state" {
                let initialDocument = document [] [ leaf "alpha"; leaf "beta" ]

                let initial =
                    match SvgDocumentInteraction.tryCreate 3 SvgAffine.identity initialDocument with
                    | Ok state -> state
                    | Error error -> failtestf "initial document state failed: %A" error

                let selected =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.SelectSemantic(3, "semantic:beta"))
                        initial

                Expect.isNone selected.Error "known semantic identity selects"

                let invalidCandidate =
                    { initialDocument with
                        Children = [ leaf "duplicate"; leaf "duplicate" ]
                    }

                let rejected =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.ReplaceDocument(3, 4, invalidCandidate))
                        selected.State

                Expect.isSome rejected.Error "invalid edit is refused"
                Expect.equal rejected.State selected.State "invalid edit is atomic and leaves all state unchanged"

                let replacement = document [] [ leaf "beta"; leaf "gamma" ]

                let replaced =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.ReplaceDocument(3, 4, replacement))
                        selected.State

                Expect.isNone replaced.Error "valid increasing replacement applies"
                Expect.equal replaced.State.SelectedSemanticId (Some "semantic:beta") "surviving selection is retained"
                Expect.equal replaced.State.FocusedSemanticId (Some "semantic:beta") "surviving focus is retained"
                Expect.equal replaced.State.UndoDocuments [ initialDocument ] "prior document enters undo history"
                Expect.isEmpty replaced.State.RedoDocuments "replacement clears redo history"

                let stale =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.SelectSemantic(3, "semantic:gamma"))
                        replaced.State

                Expect.equal stale.State replaced.State "stale selection cannot mutate state"

                Expect.equal
                    stale.Error
                    (Some(SvgDocumentInteractionError.StaleRevision(3, 4)))
                    "stale error is explicit"
            }

            test "undo redo and play snapshot are real immutable reducer witnesses" {
                let first = document [] [ leaf "first" ]
                let second = document [] [ leaf "second" ]

                let initial =
                    match SvgDocumentInteraction.tryCreate 0 SvgAffine.identity first with
                    | Ok state -> state
                    | Error error -> failtestf "initial document state failed: %A" error

                let replaced =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.ReplaceDocument(0, 1, second)) initial

                let undone =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.Undo 1) replaced.State

                Expect.equal undone.State.Revision 2 "undo creates a new ordered revision"
                Expect.equal undone.State.Document first "undo restores the prior accepted document"

                let redone =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.Redo 2) undone.State

                Expect.equal redone.State.Revision 3 "redo creates a new ordered revision"
                Expect.equal redone.State.Document second "redo restores the later accepted document"

                let captured =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.CapturePointer(3, 91)) redone.State

                let snapshotted =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.TakePlaySnapshot 3) captured.State

                let snapshot =
                    snapshotted.State.PlaySnapshot
                    |> Option.defaultWith (fun () -> failtest "snapshot missing")

                Expect.equal snapshot.SourceRevision 3 "snapshot binds the accepted source revision"

                Expect.equal
                    (SvgDocument.deserialize snapshot.SerializedDocument)
                    (Ok second)
                    "snapshot contains canonical immutable document bytes"

                let later =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.ReplaceDocument(3, 4, first))
                        snapshotted.State

                Expect.equal
                    later.State.CapturedPointerId
                    (Some 91)
                    "document edits preserve pointer capture independently"

                let serializedSecond =
                    match SvgDocument.serialize second with
                    | Ok value -> value
                    | Error issues -> failtestf "second document failed to serialize: %A" issues

                Expect.equal snapshot.SerializedDocument serializedSecond "later edits do not mutate the play snapshot"
            }

            test "document camera focus and history failures are guarded no-ops" {
                let visible = leaf "visible"
                let hidden = { leaf "hidden" with Visible = false }

                let initial =
                    match SvgDocumentInteraction.tryCreate 0 SvgAffine.identity (document [] [ visible; hidden ]) with
                    | Ok state -> state
                    | Error error -> failtestf "initial document state failed: %A" error

                let focused =
                    SvgDocumentInteraction.update (SvgDocumentInteractionMessage.FocusNext 0) initial

                Expect.equal
                    focused.State.FocusedSemanticId
                    (Some "semantic:visible")
                    "focus visits visible semantic identities only"

                let hiddenSelection =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.SelectSemantic(0, "semantic:hidden"))
                        focused.State

                Expect.equal hiddenSelection.State focused.State "hidden semantic identity is not interactive"

                let singular =
                    SvgDocumentInteraction.update
                        (SvgDocumentInteractionMessage.SetCamera(0, SvgAffine.scale 0.0 1.0))
                        focused.State

                Expect.equal singular.State focused.State "singular camera is a no-op"

                Expect.equal
                    singular.Error
                    (Some SvgDocumentInteractionError.InvalidCamera)
                    "singular camera is explicit"

                Expect.equal
                    (SvgDocumentInteraction.update (SvgDocumentInteractionMessage.Undo 0) focused.State).Error
                    (Some SvgDocumentInteractionError.NothingToUndo)
                    "empty undo is explicit"

                Expect.equal
                    (SvgDocumentInteraction.update (SvgDocumentInteractionMessage.Redo 0) focused.State).Error
                    (Some SvgDocumentInteractionError.NothingToRedo)
                    "empty redo is explicit"
            }
        ]
