module FsiSurfaceParserTests

open Expecto
open FsiSurface

[<Tests>]
let parserTests =
    testList
        "api-surface .fsi parser"
        [ test "an attributed recursive continuation keeps its declared name and kind" {
              let nodes, unparsed =
                  parseFsi
                      [ "type SvgElement ="
                        "    { Content: SvgElementContent }"
                        ""
                        "and [<RequireQualifiedAccess>] SvgElementContent ="
                        "    | SceneLeaf" ]

              Expect.isEmpty unparsed "the supported declaration shape is fully classified"
              Expect.equal
                  (nodes |> List.map (fun n -> n.Kind, n.Name))
                  [ "type", "SvgElement"; "and", "SvgElementContent" ]
                  "the attribute is signature text, not part of the continuation's lookup key"
          } ]
