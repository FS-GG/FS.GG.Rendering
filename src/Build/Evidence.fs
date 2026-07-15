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

    // Recognized readiness artifacts: relative path under readiness/, evidence kind, the tokens the
    // artifact must contain to be well-formed (per template/base/docs/evidence-formats.md and
    // contracts/evidence-output-contract.md; an empty token list means "present and non-empty"), and
    // whether the artifact is a REQUIRED baseline — its absence is a product-evidence defect, not an
    // absent-optional. The required set is the headless baseline every profile produces
    // (evidence-output-contract.md §EvidenceGraph "required-for-profile"): the deterministic layout
    // and scene evidence. All richer artifacts (launch/image/screenshot/window/…) remain optional —
    // presence is profile-dependent and the gate graphs what exists. Without a required floor the
    // audit is fail-open on absent: an empty readiness/ audits PASS (F-BUILD-1).
    let recognized: (string * string * string list * bool) list =
        [ "layout-evidence.txt", "layout", [], true
          "headless-scene-evidence.txt", "scene", [], true
          "evidence-launch-mode.txt", "launch", [], false
          "game-screenshot-evidence.txt", "screenshot", [], false
          "game-pixel-readback-evidence.txt", "pixel-readback", [], false
          "bounded-viewer-smoke.txt", "bounded-smoke", [], false
          "bounded-viewer-frame-diagnostics.txt", "bounded-smoke", [], false
          "window-diagnostics.txt", "window-diagnostics", [ "diagnostic-class=" ], false
          "window-options.txt", "window-options", [ "option=" ], false
          "interactive-visible-window.md",
          "window-visibility",
          [ "status"; "mode"; "window-visible"; "accessible-window"; "first-frame-presented"; "self-closed-for-evidence" ],
          false
          "window-state-diagnostics.md",
          "window-diagnostics",
          [ "native-handle"; "visible"; "focusable"; "renderable-surface"; "input-devices" ],
          false
          "real-image-evidence.md",
          "image",
          [ "evidence-kind"; "status"; "artifact-decodable"; "proves-scene-rendering"; "proves-desktop-visibility" ],
          false
          "generated-validation.md",
          "generated-validation",
          [ "exact-package-match"; "generated-tests-ran"; "authoritative"; "failure-class" ],
          false ]

    // The required baseline artifacts (relative path under readiness/), derived from `recognized`.
    // The audit and graph both fail when one of these is absent (evidence-output-contract.md).
    let requiredArtifacts: string list =
        recognized
        |> List.choose (fun (rel, _, _, required) -> if required then Some rel else None)

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

    // Required baseline artifacts (`readiness/`-relative) that no sensed node covers. A required
    // artifact that is present-but-malformed is NOT reported here — its `PresentInvalid` node is
    // already a failure via the token contract; this reports only genuine absence, so a malformed
    // baseline is not double-counted.
    let missingRequired (nodes: EvidenceNode list) : string list =
        let presentPaths = nodes |> List.map (fun n -> n.ArtifactPath) |> Set.ofList

        requiredArtifacts
        |> List.map (fun rel -> "readiness/" + rel)
        |> List.filter (fun path -> not (presentPaths.Contains path))

    let writeReport (dir: string) (relName: string) (body: string) =
        let target = Path.Combine(readinessDir dir, relName)

        match Path.GetDirectoryName target with
        | null -> ()
        | parent -> Directory.CreateDirectory parent |> ignore

        File.WriteAllText(target, body)

module Graph =

    let sense (dir: string) : EvidenceNode list =
        Sensing.recognized
        |> List.choose (fun (rel, kind, tokens, _) ->
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
        line "product's readiness surface. The graph reflects the artifacts that exist at gate time."
        line "Absent OPTIONAL artifacts (interactive launch/image/window/…, profile-dependent) are not"
        line "failures. The required headless baseline (layout + scene evidence) MUST be present, however —"
        line "its absence is a product-evidence defect (evidence-output-contract.md §EvidenceGraph)."
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

        line ""
        line "## Required baseline"
        line ""

        match Sensing.missingRequired nodes with
        | [] -> line "_required headless baseline present (layout + scene evidence)_"
        | missing -> missing |> List.iter (fun p -> line (sprintf "- MISSING (required): `%s`" p))

        sb.ToString()

module Audit =

    let evaluate (nodes: EvidenceNode list) : Verdict =
        let malformed =
            nodes
            |> List.choose (fun n ->
                match n.State with
                | EvidenceState.PresentInvalid reason -> Some(sprintf "%s (%s)" n.ArtifactPath reason)
                | EvidenceState.PresentValid -> None)

        // Required-floor (F-BUILD-1): an absent baseline artifact is a product-evidence defect, not an
        // absent-optional. Without this the audit is fail-open — an empty readiness/ yields no failures
        // and audits PASS, so a product emitting zero evidence passes the gate named Audit green.
        let absentRequired =
            Sensing.missingRequired nodes
            |> List.map (fun path -> sprintf "%s (required baseline evidence absent)" path)

        match malformed @ absentRequired with
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
            line "artifact is malformed, or a required baseline artifact is absent). It is NOT a"
            line "framework/feed engine-resolution condition —"
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

            // Non-0 also when a required baseline artifact is absent, matching the audit's floor and
            // the contract ("non-0 when a required-for-profile artifact is missing/malformed").
            let hasMissingRequired = not (List.isEmpty (Sensing.missingRequired nodes))

            if hasInvalid || hasMissingRequired then 1 else 0
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
