module Issue1030ViewImageSizeTests

open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value

let private read (relative: string) =
    File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))

[<Tests>]
let viewImageSizeContract =
    testList "issue-1030 explicit view-image size contract" [
        test "packed template emits explicit positive dimensions and PNG-header metadata" {
            let commands = read "template/base/src/Product/EvidenceCommands.fs"

            [ "viewImageAtSize"
              "width <= 0 || height <= 0"
              "requestedPixels > maxViewImagePixels"
              "diagnostic-category=resource-limit"
              "tryPngDimensions pngBytes"
              "evidenceField \"requested-size\""
              "evidenceField \"actual-size\""
              "evidenceField \"dimensions-match\""
              "diagnostic-category=invalid-dimensions"
              "\"--view-image\" :: path :: width :: height" ]
            |> List.iter (fun token ->
                Expect.stringContains commands token $"generated command source carries `{token}`")
        }

        test "generated-product tests exercise two sizes and invalid-size rejection" {
            let behavior = read "template/base/tests/Product.Tests/BehaviorTests.fs"

            [ "[ 320, 200; 901, 507 ]"
              "readBigEndianInt32 pngBytes 16"
              "readBigEndianInt32 pngBytes 20"
              "dimensions-match=True"
              "[ \"--view-image\"; invalidPath; \"0\"; \"720\" ]"
              "Expect.equal invalid (Some 1)"
              "\"2000000000\"; \"2000000000\""
              "Expect.equal oversized (Some 1)"
              "an oversized request writes no image" ]
            |> List.iter (fun token ->
                Expect.stringContains behavior token $"generated behavior tests carry `{token}`")
        }

        test "generated guidance documents the default and explicit forms" {
            let docs =
                [ read "template/base/README.md"
                  read "template/base/docs/product.md"
                  read "template/product-skills/fs-gg-testing/SKILL.md" ]
                |> String.concat "\n"

            Expect.stringContains docs "--view-image <path> <width> <height>" "guidance documents the explicit form"
            Expect.stringContains docs "1280x720" "guidance documents the deterministic default"
            Expect.stringContains docs "PNG-header" "guidance identifies the actual-dimension authority"
        }
    ]
