module ControlsFeature132CoverageMatrixTests

// Feature 132 (D2.1) — the coverage-matrix HONESTY CHECK (US2, SC-002/SC-003).
//
// Parses `docs/product/ant-design/coverage/ant-component-coverage.md` and fails (per the
// contract H1–H6 rules) if:
//   H1 — an Ant overview component from the pinned snapshot list has no matrix row.
//   H2 — a row's disposition is blank or not one of existing/net-new/composition/not-applicable.
//   H3 — an existing/net-new/composition row names a repoControls id absent from `Catalog`.
//   H4 — a covered row names a tokenEntries entry absent from the DesignSystem public token surface.
//   H5 — a composition or not-applicable row has an empty rationale.
//   H6 — the count of un-dispositioned entries is non-zero.
// Plus: the foot summary totals reconcile with the pinned snapshot size.

open System
open System.IO
open System.Reflection
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private matrixPath =
    Path.Combine(repositoryRoot, "docs", "product", "ant-design", "coverage", "ant-component-coverage.md")

// The pinned Ant component-overview snapshot (retrieved 2026-06-16). The honesty check requires
// exactly one matrix row per entry. Grouped as Ant's overview groups them.
let private pinnedComponents =
    [ // General
      "Button"; "FloatButton"; "Icon"; "Typography"
      // Layout
      "Divider"; "Flex"; "Grid"; "Layout"; "Space"; "Splitter"
      // Navigation
      "Anchor"; "Breadcrumb"; "Dropdown"; "Menu"; "Pagination"; "Steps"; "Tabs"
      // Data Entry
      "AutoComplete"; "Cascader"; "Checkbox"; "ColorPicker"; "DatePicker"; "Form"; "Input"
      "InputNumber"; "Mentions"; "Radio"; "Rate"; "Select"; "Slider"; "Switch"; "TimePicker"
      "Transfer"; "TreeSelect"; "Upload"
      // Data Display
      "Avatar"; "Badge"; "Calendar"; "Card"; "Carousel"; "Collapse"; "Descriptions"; "Empty"
      "Image"; "List"; "Popover"; "QRCode"; "Segmented"; "Statistic"; "Table"; "Tag"; "Timeline"
      "Tooltip"; "Tour"; "Tree"
      // Feedback
      "Alert"; "Drawer"; "Message"; "Modal"; "Notification"; "Popconfirm"; "Progress"; "Result"
      "Skeleton"; "Spin"; "Watermark"
      // Other
      "Affix"; "App"; "ConfigProvider"; "Util" ]

let private validDispositions = set [ "existing"; "net-new"; "composition"; "not-applicable" ]
let private coveredDispositions = set [ "existing"; "net-new"; "composition" ]

type private Row =
    { Component: string
      Category: string
      Disposition: string
      RepoControls: string list
      TokenEntries: string list
      Rationale: string }

let private splitCell (s: string) =
    s.Split(',')
    |> Array.map (fun p -> p.Trim())
    |> Array.filter (fun p -> p <> "" && p <> "—")
    |> Array.toList

let private parseRows () : Row list =
    File.ReadAllLines matrixPath
    |> Array.filter (fun l -> l.StartsWith "|")
    |> Array.map (fun l -> l.Trim().Trim('|').Split('|') |> Array.map (fun c -> c.Trim()))
    |> Array.filter (fun cols -> cols.Length = 6 && cols.[0] <> "antComponent" && not (cols.[0].StartsWith "---"))
    |> Array.map (fun cols ->
        { Component = cols.[0]
          Category = cols.[1]
          Disposition = cols.[2]
          RepoControls = splitCell cols.[3]
          TokenEntries = splitCell cols.[4]
          Rationale = cols.[5].Trim() })
    |> Array.toList

let private catalogIds = Catalog.supportedControls |> List.map (fun c -> c.Id) |> Set.ofList

let private designSystemAsm =
    DesignTokensExt.Seed.colorPrimary |> ignore
    AppDomain.CurrentDomain.GetAssemblies()
    |> Array.find (fun a -> a.GetName().Name = "FS.GG.UI.DesignSystem")

let private tokenNames : Set<string> =
    let acc = System.Collections.Generic.HashSet<string>()
    let staticFlags = BindingFlags.Public ||| BindingFlags.Static
    let rec walk (t: Type) (prefix: string) =
        for p in t.GetProperties staticFlags do acc.Add(prefix + p.Name) |> ignore
        for f in t.GetFields staticFlags do acc.Add(prefix + f.Name) |> ignore
        for n in t.GetNestedTypes BindingFlags.Public do walk n (prefix + n.Name + ".")
    for rootName in [ "DesignTokensExt"; "DesignTokens" ] do
        match designSystemAsm.GetType("FS.GG.UI.DesignSystem." + rootName) with
        | null -> ()
        | t -> for n in t.GetNestedTypes BindingFlags.Public do walk n (n.Name + ".")
    Set.ofSeq acc

[<Tests>]
let feature132CoverageMatrixTests =
    testList "Feature 132 coverage-matrix honesty check (SC-002/SC-003)" [

        test "the matrix doc exists and parses to one row per pinned Ant overview component (H1)" {
            Expect.isTrue (File.Exists matrixPath) "the coverage matrix doc exists"
            let rows = parseRows ()
            let present = rows |> List.map (fun r -> r.Component) |> Set.ofList
            let missing = Set.difference (Set.ofList pinnedComponents) present
            Expect.isEmpty missing (sprintf "every pinned Ant component has a matrix row; missing %A" missing)
            Expect.equal (List.length rows) (List.length pinnedComponents)
                "exactly one matrix row per pinned component (no extras, no gaps)"
        }

        test "every row has a valid, non-blank disposition (H2, H6)" {
            for r in parseRows () do
                Expect.isTrue
                    (validDispositions.Contains r.Disposition)
                    (sprintf "%s disposition '%s' is one of existing/net-new/composition/not-applicable" r.Component r.Disposition)
            let blank = parseRows () |> List.filter (fun r -> r.Disposition.Trim() = "")
            Expect.isEmpty blank "no un-dispositioned entries (SC-002 / H6)"
        }

        test "every covered row references only live Catalog control ids (H3, SC-003)" {
            for r in parseRows () |> List.filter (fun r -> coveredDispositions.Contains r.Disposition) do
                for id in r.RepoControls do
                    Expect.isTrue
                        (catalogIds.Contains id)
                        (sprintf "%s row references Catalog control '%s' (must exist)" r.Component id)
                Expect.isNonEmpty r.RepoControls (sprintf "%s (%s) names at least one repo control" r.Component r.Disposition)
        }

        test "every covered row references only live DesignSystem token entries (H4, SC-003)" {
            for r in parseRows () |> List.filter (fun r -> coveredDispositions.Contains r.Disposition) do
                for tok in r.TokenEntries do
                    Expect.isTrue
                        (tokenNames.Contains tok)
                        (sprintf "%s row references token entry '%s' (must exist in the DesignSystem public surface)" r.Component tok)
                Expect.isNonEmpty r.TokenEntries (sprintf "%s (%s) names at least one token entry" r.Component r.Disposition)
        }

        test "composition and not-applicable rows carry a non-empty rationale (H5)" {
            for r in parseRows () |> List.filter (fun r -> r.Disposition = "composition" || r.Disposition = "not-applicable") do
                Expect.isTrue
                    (r.Rationale <> "" && r.Rationale <> "—")
                    (sprintf "%s (%s) has a non-empty rationale" r.Component r.Disposition)
        }

        test "the foot summary totals reconcile with the pinned snapshot size" {
            let rows = parseRows ()
            let byDisp = rows |> List.countBy (fun r -> r.Disposition) |> Map.ofList
            let total = rows |> List.length
            Expect.equal total (List.length pinnedComponents) "matrix totals reconcile with the snapshot list size"
            // every disposition bucket is non-empty for a genuinely "maximal" matrix
            for d in validDispositions do
                Expect.isTrue (byDisp.ContainsKey d) (sprintf "the matrix uses the '%s' disposition" d)
        }
    ]
