namespace FS.GG.UI.Build.Evidence

// Feature 202: implementation of the in-process governance engine. Plain F# (Principle III):
// read the readiness surface, fold it into evidence nodes, render markdown, return an exit code.
// No reflection here (that lives only in the consumer build.fsx); no external process; no
// dependency beyond FSharp.Core. Visibility is owned by Evidence.fsi — helpers below that are
// not in the signature (the `Sensing` module) are private to the assembly by construction.

open System
open System.IO
open System.Text

[<RequireQualifiedAccess>]
type EvidenceState =
    | PresentValid
    | PresentInvalid of reason: string

type EvidenceNode =
    { ArtifactPath: string
      Kind: string
      State: EvidenceState }

[<RequireQualifiedAccess>]
type Verdict =
    | Pass
    | Fail of reason: string

// Hidden from Evidence.fsi → private to the assembly. Shared sensing/IO used by Graph and Audit.
module Sensing =

    // Recognized readiness artifacts: relative path under readiness/, evidence kind, and the
    // tokens the artifact must contain to be well-formed (per template/base/docs/evidence-formats.md
    // and contracts/evidence-output-contract.md). An empty token list means "present and non-empty".
    let recognized: (string * string * string list) list =
        [ "layout-evidence.txt", "layout", []
          "headless-scene-evidence.txt", "scene", []
          "evidence-launch-mode.txt", "launch", []
          "game-screenshot-evidence.txt", "screenshot", []
          "game-pixel-readback-evidence.txt", "pixel-readback", []
          "bounded-viewer-smoke.txt", "bounded-smoke", []
          "bounded-viewer-frame-diagnostics.txt", "bounded-smoke", []
          "window-diagnostics.txt", "window-diagnostics", [ "diagnostic-class=" ]
          "window-options.txt", "window-options", [ "option=" ]
          "interactive-visible-window.md",
          "window-visibility",
          [ "status"; "mode"; "window-visible"; "accessible-window"; "first-frame-presented"; "self-closed-for-evidence" ]
          "window-state-diagnostics.md",
          "window-diagnostics",
          [ "native-handle"; "visible"; "focusable"; "renderable-surface"; "input-devices" ]
          "real-image-evidence.md",
          "image",
          [ "evidence-kind"; "status"; "artifact-decodable"; "proves-scene-rendering"; "proves-desktop-visibility" ]
          "generated-validation.md",
          "generated-validation",
          [ "exact-package-match"; "generated-tests-ran"; "authoritative"; "failure-class" ] ]

    let readinessDir (dir: string) = Path.Combine(dir, "readiness")

    // All files present under readiness/ (product-relative, forward-slashed), for the graph's raw
    // surface listing. Returns [] when readiness/ does not exist.
    let presentFiles (dir: string) : string list =
        let root = readinessDir dir

        if not (Directory.Exists root) then
            []
        else
            Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            |> Array.map (fun f -> Path.GetRelativePath(dir, f).Replace('\\', '/'))
            |> Array.sort
            |> Array.toList

    // Validate one present artifact's text against its required tokens.
    let stateOf (requiredTokens: string list) (text: string) : EvidenceState =
        if String.IsNullOrWhiteSpace text then
            EvidenceState.PresentInvalid "empty artifact (no evidence content)"
        else
            match requiredTokens |> List.filter (fun t -> not (text.Contains(t, StringComparison.Ordinal))) with
            | [] -> EvidenceState.PresentValid
            | missing -> EvidenceState.PresentInvalid(sprintf "missing required token(s): %s" (String.concat ", " missing))

    let writeReport (dir: string) (relName: string) (body: string) =
        let target = Path.Combine(readinessDir dir, relName)

        match Path.GetDirectoryName target with
        | null -> ()
        | parent -> Directory.CreateDirectory parent |> ignore

        File.WriteAllText(target, body)

module Graph =

    let sense (dir: string) : EvidenceNode list =
        Sensing.recognized
        |> List.choose (fun (rel, kind, tokens) ->
            let full = Path.Combine(Sensing.readinessDir dir, rel)

            if File.Exists full then
                Some
                    { ArtifactPath = "readiness/" + rel
                      Kind = kind
                      State = Sensing.stateOf tokens (File.ReadAllText full) }
            else
                None)

    let render (dir: string) (nodes: EvidenceNode list) : string =
        let sb = StringBuilder()
        let line (s: string) = sb.AppendLine s |> ignore
        let files = Sensing.presentFiles dir

        line "# Evidence graph"
        line ""
        line "Synthesized in-process by the FS.GG.UI.Build engine (EvidenceGraph) over the generated"
        line "product's readiness surface. The graph reflects the artifacts that exist at gate time;"
        line "absent optional artifacts are not failures (the Verify gate does not pre-produce evidence)."
        line ""
        line (sprintf "- readiness files present: %d" (List.length files))
        line (sprintf "- recognized evidence nodes: %d" (List.length nodes))
        line ""
        line "## Sensed readiness files"
        line ""

        if List.isEmpty files then
            line "_none — readiness/ is empty or absent_"
        else
            files |> List.iter (fun f -> line (sprintf "- `%s`" f))

        line ""
        line "## Evidence nodes"
        line ""

        if List.isEmpty nodes then
            line "_no recognized evidence artifacts present; graphed the available surface above_"
        else
            line "| Artifact | Kind | State |"
            line "|---|---|---|"

            nodes
            |> List.iter (fun n ->
                let state =
                    match n.State with
                    | EvidenceState.PresentValid -> "present-valid"
                    | EvidenceState.PresentInvalid reason -> sprintf "present-invalid: %s" reason

                line (sprintf "| `%s` | %s | %s |" n.ArtifactPath n.Kind state))

        sb.ToString()

module Audit =

    let evaluate (nodes: EvidenceNode list) : Verdict =
        let failures =
            nodes
            |> List.choose (fun n ->
                match n.State with
                | EvidenceState.PresentInvalid reason -> Some(sprintf "%s (%s)" n.ArtifactPath reason)
                | EvidenceState.PresentValid -> None)

        match failures with
        | [] -> Verdict.Pass
        | reasons -> Verdict.Fail(sprintf "product-evidence defect: %s" (String.concat "; " reasons))

    let render (verdict: Verdict) (nodes: EvidenceNode list) : string =
        let sb = StringBuilder()
        let line (s: string) = sb.AppendLine s |> ignore

        let verdictToken =
            match verdict with
            | Verdict.Pass -> "PASS"
            | Verdict.Fail _ -> "FAIL"

        line "# Evidence audit"
        line ""
        line (sprintf "verdict=%s" verdictToken)
        line ""
        line "Feature-local merge-gate audit record produced in-process by FS.GG.UI.Build (EvidenceAudit)."
        line (sprintf "- evidence nodes audited: %d" (List.length nodes))
        line ""

        match verdict with
        | Verdict.Pass ->
            line "All present evidence artifacts satisfy their token contract. Engine-resolution"
            line "(framework/feed) failures are surfaced separately by build.fsx before the engine runs."
        | Verdict.Fail reason ->
            line "failure-class=product-evidence-defect"
            line (sprintf "reason: %s" reason)
            line ""
            line "This verdict concerns the generated product's own evidence integrity (a present"
            line "artifact is malformed). It is NOT a framework/feed engine-resolution condition —"
            line "those are reported by build.fsx (naming FS.GG.UI.Build <version> and the feed/path"
            line "searched) before the engine is invoked."

        sb.ToString()

module GeneratedRunner =

    let run (target: string) (dir: string) : int =
        let nodes = Graph.sense dir

        match target with
        | "EvidenceGraph" ->
            Sensing.writeReport dir "evidence-graph.md" (Graph.render dir nodes)

            let hasInvalid =
                nodes
                |> List.exists (fun n ->
                    match n.State with
                    | EvidenceState.PresentInvalid _ -> true
                    | EvidenceState.PresentValid -> false)

            if hasInvalid then 1 else 0
        | "EvidenceAudit" ->
            let verdict = Audit.evaluate nodes
            Sensing.writeReport dir "evidence-audit.md" (Audit.render verdict nodes)

            match verdict with
            | Verdict.Pass -> 0
            | Verdict.Fail _ -> 1
        | other ->
            eprintfn
                "FS.GG.UI.Build: unknown evidence target '%s' (expected 'EvidenceGraph' or 'EvidenceAudit')."
                other

            2
