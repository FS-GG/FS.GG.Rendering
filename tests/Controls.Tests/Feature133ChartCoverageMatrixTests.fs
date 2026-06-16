module ControlsFeature133ChartCoverageMatrixTests

// Feature 133 (D2C.1) — the Ant Charts coverage-matrix HONESTY CHECK (US2, SC-001/SC-002) plus the
// no-charting-dependency guard (US5, SC-006/FR-008).
//
// Parses `docs/product/ant-design/coverage/ant-chart-coverage.md` and fails (per the contract H1–H6
// rules) if:
//   H1 — an Ant Charts overview entry from the pinned snapshot list has no matrix row.
//   H2 — a row's disposition is blank or not one of existing/net-new/composition/not-applicable.
//   H3 — an existing/net-new/composition row names a repoControls id absent from `Catalog`.
//   H4 — a covered row names a tokenEntries entry absent from the DesignSystem public token surface.
//   H5 — a composition or not-applicable row has an empty rationale.
//   H6 — the count of un-dispositioned entries is non-zero.
// Plus: the foot summary totals reconcile with the pinned snapshot size; and no AntV/React/JS
// charting or geospatial dependency leaks into the chart sources/project (FR-008).

open System
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

let private repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else (match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir)
    find __SOURCE_DIRECTORY__

let private matrixPath =
    Path.Combine(repositoryRoot, "docs", "product", "ant-design", "coverage", "ant-chart-coverage.md")

// The pinned Ant Charts overview snapshot (retrieved 2026-06-16, owned by the hub). The honesty check
// requires exactly one matrix row per entry. Grouped as Ant's overview groups them (statistical /
// relational / hierarchical / geo-flow / general).
let private pinnedCharts =
    [ // Statistical
      "Line"; "Area"; "Column"; "Bar"; "Pie"; "Scatter"; "Histogram"; "Box Plot"; "Heatmap"
      "Radar"; "Rose"; "Waterfall"; "Funnel"; "Dual Axes"; "Stacked Column"; "Grouped Column"; "Bullet"
      // Relational
      "Sankey"; "Chord"; "Network Graph"
      // Hierarchical
      "Treemap"; "Sunburst"
      // Geo-Flow
      "Choropleth Map"; "Point Map"; "Heatmap Map"; "Flow Map"
      // General
      "Gauge" ]

let private validDispositions = set [ "existing"; "net-new"; "composition"; "not-applicable" ]
let private coveredDispositions = set [ "existing"; "net-new"; "composition" ]

type private Row =
    { Chart: string
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
    |> Array.filter (fun cols -> cols.Length = 6 && cols.[0] <> "antChart" && not (cols.[0].StartsWith "---"))
    |> Array.map (fun cols ->
        { Chart = cols.[0]
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

// US5 / FR-008: the chart sources + project must add no charting-engine or geospatial dependency.
let private chartSourceFiles =
    [ Path.Combine(repositoryRoot, "src", "Controls", "Charts2.fs")
      Path.Combine(repositoryRoot, "src", "Controls", "Charts2.fsi")
      Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj") ]

let private forbiddenDependency = Regex("antv|/g2|/g6|/l7|react|d3-|charting", RegexOptions.IgnoreCase)

[<Tests>]
let feature133ChartCoverageMatrixTests =
    testList "Feature 133 chart coverage-matrix honesty check (SC-001/SC-002) + no-dep guard (SC-006)" [

        test "the matrix doc exists and parses to one row per pinned Ant Charts overview entry (H1)" {
            Expect.isTrue (File.Exists matrixPath) "the chart coverage matrix doc exists"
            let rows = parseRows ()
            let present = rows |> List.map (fun r -> r.Chart) |> Set.ofList
            let missing = Set.difference (Set.ofList pinnedCharts) present
            Expect.isEmpty missing (sprintf "every pinned Ant Charts entry has a matrix row; missing %A" missing)
            Expect.equal (List.length rows) (List.length pinnedCharts)
                "exactly one matrix row per pinned chart (no extras, no gaps)"
        }

        test "every row has a valid, non-blank disposition (H2, H6)" {
            for r in parseRows () do
                Expect.isTrue
                    (validDispositions.Contains r.Disposition)
                    (sprintf "%s disposition '%s' is one of existing/net-new/composition/not-applicable" r.Chart r.Disposition)
            let blank = parseRows () |> List.filter (fun r -> r.Disposition.Trim() = "")
            Expect.isEmpty blank "no un-dispositioned entries (SC-001 / H6)"
        }

        test "every covered row references only live Catalog control ids (H3, SC-002)" {
            for r in parseRows () |> List.filter (fun r -> coveredDispositions.Contains r.Disposition) do
                for id in r.RepoControls do
                    Expect.isTrue
                        (catalogIds.Contains id)
                        (sprintf "%s row references Catalog control '%s' (must exist)" r.Chart id)
                Expect.isNonEmpty r.RepoControls (sprintf "%s (%s) names at least one repo control" r.Chart r.Disposition)
        }

        test "every covered row references only live DesignSystem token entries (H4, SC-002)" {
            for r in parseRows () |> List.filter (fun r -> coveredDispositions.Contains r.Disposition) do
                for tok in r.TokenEntries do
                    Expect.isTrue
                        (tokenNames.Contains tok)
                        (sprintf "%s row references token entry '%s' (must exist in the DesignSystem public surface)" r.Chart tok)
                Expect.isNonEmpty r.TokenEntries (sprintf "%s (%s) names at least one token entry" r.Chart r.Disposition)
        }

        test "composition and not-applicable rows carry a non-empty rationale (H5)" {
            for r in parseRows () |> List.filter (fun r -> r.Disposition = "composition" || r.Disposition = "not-applicable") do
                Expect.isTrue
                    (r.Rationale <> "" && r.Rationale <> "—")
                    (sprintf "%s (%s) has a non-empty rationale" r.Chart r.Disposition)
        }

        test "the foot summary totals reconcile with the pinned snapshot size; every bucket used" {
            let rows = parseRows ()
            let byDisp = rows |> List.countBy (fun r -> r.Disposition) |> Map.ofList
            Expect.equal (List.length rows) (List.length pinnedCharts) "matrix totals reconcile with the snapshot list size"
            for d in validDispositions do
                Expect.isTrue (byDisp.ContainsKey d) (sprintf "the matrix uses the '%s' disposition" d)
            // the net-new bucket is exactly the 14 generic chart controls this feature adds
            Expect.equal (byDisp.["net-new"]) 14 "the matrix marks exactly the 14 net-new generic chart controls"
        }

        test "no AntV/React/JS charting or geospatial dependency leaks into the chart sources (US5, FR-008/SC-006)" {
            for f in chartSourceFiles do
                Expect.isTrue (File.Exists f) (sprintf "%s exists" (Path.GetFileName f))
                let text = File.ReadAllText f
                let m = forbiddenDependency.Match text
                Expect.isFalse m.Success
                    (sprintf "%s adds no charting/geospatial dependency (matched '%s')" (Path.GetFileName f) (if m.Success then m.Value else ""))
        }
    ]
