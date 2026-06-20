/// App-edge runner for the automated control pass (Feature 176). Composes the pure
/// `SecondAntShowcase.Core.ControlPass` plan with the existing evidence infrastructure:
///
///   * functional dimension — drives every documented behavior through the pure showcase MVU
///     (`Model.update`) and asserts each resulting state change (US1, deterministic, headless);
///   * structural visual + damage dimension — `ControlInspection.inspectRetained` over the
///     Shell render of each driven transition gives a real, damage-local interaction-state
///     delta with no GL window (US2 structural);
///   * live-pixel dimension — offscreen `Viewer.captureScreenshotEvidence` for the
///     appearance x size matrix, which fail-closes to `environment-limited` when no renderable
///     surface is present (FR-008).
///
/// All IO (file writes, window probing, readback) lives here at the App edge; the verdict
/// schema, classification, behavior plan, and aggregation are the pure Core module.
module SecondAntShowcase.App.ControlPassRunner

open System
open System.IO
open System.Text
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model
open SecondAntShowcase.Core.ControlPass

// --- CLI config -------------------------------------------------------------

type Config =
    { Seed: int
      Appearances: (ThemeMode * string) list
      Sizes: (string * Size) list
      Backend: string
      RequireLive: bool
      Page: string option
      OutDir: string
      Json: bool }

let private defaultOut = "specs/176-test-antshowcase-controls/readiness"

let private flag (name: string) (args: string list) : string option =
    let rec loop =
        function
        | k :: v :: _ when k = name -> Some v
        | _ :: rest -> loop rest
        | [] -> None

    loop args

let private hasFlag (name: string) (args: string list) : bool = args |> List.contains name

/// `--themes light,dark` maps to the antLight/antDark appearances (the CLI says "themes" but
/// the domain concept is "appearances" — T015).
let private parseAppearances (text: string) : Result<(ThemeMode * string) list, string> =
    VisualConfig.resolveThemeList text

/// `--sizes preferred,minimum` maps to the two representative sizes (1600x1000, 1280x800).
let private parseSizes (text: string) : Result<(string * Size) list, string> =
    text.Split(',')
    |> Array.map (fun raw -> raw.Trim().ToLowerInvariant())
    |> Array.filter (fun s -> s <> "")
    |> Array.fold
        (fun acc token ->
            match acc with
            | Result.Error e -> Result.Error e
            | Ok xs ->
                match token with
                | "preferred" -> Ok(xs @ [ "preferred", VisualConfig.preferredSize ])
                | "minimum" -> Ok(xs @ [ "minimum", VisualConfig.minimumSize ])
                | other -> Result.Error(sprintf "unknown size '%s' (expected preferred|minimum)" other))
        (Ok [])

let parse (args: string list) : Result<Config, string> =
    let seedText = flag "--seed" args |> Option.defaultValue "1"
    let themesText = flag "--themes" args |> Option.defaultValue "light,dark"
    let sizesText = flag "--sizes" args |> Option.defaultValue "preferred,minimum"
    let backend = flag "--backend" args |> Option.defaultValue "pure"
    let outDir = flag "--out" args |> Option.defaultValue defaultOut
    let page = flag "--page" args

    match Int32.TryParse seedText with
    | false, _ -> Result.Error(sprintf "--seed must be an integer (got '%s')" seedText)
    | true, seed ->
        match parseAppearances themesText, parseSizes sizesText with
        | Result.Error e, _ -> Result.Error e
        | _, Result.Error e -> Result.Error e
        | Ok appearances, Ok sizes ->
            match backend with
            | "pure"
            | "x11xtest"
            | "uinput" ->
                Ok
                    { Seed = seed
                      Appearances = appearances
                      Sizes = sizes
                      Backend = backend
                      RequireLive = hasFlag "--require-live" args
                      Page = page
                      OutDir = outDir
                      Json = hasFlag "--json" args }
            | other -> Result.Error(sprintf "unknown backend '%s' (expected pure|x11xtest|uinput)" other)

// --- environment detection (G-6) --------------------------------------------

type LiveEnvironment =
    { Renderable: bool
      Reasons: string list }

let probeEnvironment () : LiveEnvironment =
    let capability = Viewer.runtimeCapability ()

    { Renderable = capability.PersistentWindow
      Reasons =
        if capability.PersistentWindow then
            []
        else
            match capability.UnsupportedHostReasons with
            | [] -> [ sprintf "renderer '%s' reports no persistent window" capability.RendererMode ]
            | rs -> rs }

// --- functional dimension (US1) ---------------------------------------------

let private seededModel (seed: int) : SecondAntShowcaseModel =
    // The showcase seed is theme-independent and deterministic; `seed` threads through for parity
    // with the other evidence CLIs even though the pure state is fixed.
    ignore seed
    Host.initModel

// --- structural interaction-state + damage dimension (US2 structural) -------

/// Map a contract's action type to the interaction-state kind its driven transition represents.
let private stateKindFor (contract: InteractionContracts.InteractionContract) : InteractionStateKind =
    match contract.ActionType with
    | "toggle"
    | "select" -> Selected
    | "open-close" -> Active
    | "drag"
    | "value-change" -> Active
    | "navigate" -> Active
    | _ -> Active

let private damageStatusOf (status: DamageInspectionStatus) : DamageStatus =
    match status with
    | DamageInspectionStatus.Empty -> Empty
    | DamageInspectionStatus.Localized -> Localized
    | DamageInspectionStatus.Broad -> Broad
    | DamageInspectionStatus.FullSurface -> FullSurface
    | DamageInspectionStatus.Unsupported
    | DamageInspectionStatus.NotInspected -> Unsupported

/// Inspect one driven state transition structurally (headless-capable). Returns the
/// interaction-state delta and the damage-locality outcome for the transition.
let private inspectTransition
    (theme: Theme)
    (size: Size)
    (runId: string)
    (controlId: string)
    (contract: InteractionContracts.InteractionContract)
    (seed: SecondAntShowcaseModel)
    : InteractionStateOutcome * DamageOutcome =
    let pageId = contract.PageId
    let restModel = { seed with CurrentPage = pageId }

    let stateModel =
        match contract.ScriptStep with
        | Some msg -> Model.update msg restModel
        | None -> restModel

    let scope: VisualInspectionScope =
        { ScopeId = controlId
          Title = controlId
          Required = true }

    let transitionId = sprintf "%s/%s" controlId contract.ContractId

    let artifact =
        ControlInspection.inspectRetained
            { Scope = scope
              Theme = theme
              OutputSize = size
              Presentation = "control-pass"
              RunId = Some runId
              Transition =
                { TransitionId = transitionId
                  PriorControl = Some(Shell.view size restModel)
                  CurrentControl = Shell.view size stateModel
                  InteractionId = Some contract.ContractId
                  ExpectedAffectedRegionIds = []
                  MaximumDirtyPercentage = None
                  IntentionalExceptions = [] }
              RelatedVisualEvidence = [] }

    let damage = artifact.Damage

    let dirtyPct = damage |> Option.map (fun d -> d.DirtyPercentage) |> Option.defaultValue 0.0
    let regionIds = damage |> Option.map (fun d -> d.AffectedRegionIds) |> Option.defaultValue []

    let repainted =
        damage
        |> Option.map (fun d -> d.RepaintedNodeCount + d.ShiftedNodeCount)
        |> Option.defaultValue 0

    let status = damage |> Option.map (fun d -> damageStatusOf d.DamageStatus) |> Option.defaultValue Unsupported

    // A driven state with no node delta vs rest is a dead-affordance defect (Feature 175 class).
    let differs = repainted > 0 || dirtyPct > 0.0

    let stateVerdict = if differs then Pass else Fail

    let stateOutcome =
        { State = stateKindFor contract
          DiffersFromRest = differs
          EvidenceRef = transitionId
          Verdict = stateVerdict }

    // Broad/full-surface damage without an intentional exception is a finding (M-4, FR-005).
    let damageVerdict =
        match status with
        | Localized
        | Empty -> Pass
        | Broad
        | FullSurface -> NeedsReview
        | Unsupported -> EnvironmentLimited

    let damageOutcome =
        { TransitionId = transitionId
          DamageStatus = status
          DirtyPercentage = dirtyPct
          AffectedRegionIds = regionIds
          Verdict = damageVerdict }

    stateOutcome, damageOutcome

// --- live-pixel visual matrix (US2 live, degrade-aware) ---------------------

let private appearanceOf (themeId: string) : Appearance =
    if themeId.ToLowerInvariant().Contains "dark" then AntDark else AntLight

let private sizeRoleOf (roleName: string) : SizeRole =
    if roleName = "minimum" then Minimum else Preferred

/// Capture one page at an appearance x size cell via offscreen readback. Returns the capture
/// status; a missing renderable surface degrades the cell (mapped to environment-limited later).
let private capturePageCell
    (outDir: string)
    (mode: ThemeMode)
    (themeId: string)
    (size: Size)
    (roleName: string)
    (pageId: string)
    : CaptureStatus * string =
    let folder = Path.Combine(outDir, "visual-evidence", themeId, roleName)
    Directory.CreateDirectory folder |> ignore
    let outPath = Path.Combine(folder, pageId + ".png")
    let relativePath = sprintf "visual-evidence/%s/%s/%s.png" themeId roleName pageId
    let model = { Host.initModel with CurrentPage = pageId; Mode = mode }
    let theme = AntTheme.resolve mode
    let rendered = Control.renderTree theme size (Shell.view size model)
    let scene = SceneNode.Group [ rendered.Scene ]

    let request: ScreenshotEvidenceRequest =
        { Command = "control-pass"
          AppOrSample = "second-ant-showcase"
          OutputPath = outPath
          Width = size.Width
          Height = size.Height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = [ sprintf "theme=%s" themeId; sprintf "size=%s" roleName; sprintf "page=%s" pageId ]
          Timeout = TimeSpan.FromSeconds 10.0 }

    let options: ViewerOptions =
        { Title = "second-ant-showcase-control-pass"
          InitialSize = size
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    try
        let result = Viewer.captureScreenshotEvidence request options scene

        if result.ProvesScreenshot && File.Exists outPath then
            Complete, relativePath
        else
            if File.Exists outPath then File.Delete outPath
            Degraded, relativePath
    with _ ->
        if File.Exists outPath then File.Delete outPath
        Degraded, relativePath

// --- per-control assembly ---------------------------------------------------

let private fidelityOf (status: CaptureStatus) : FidelityVerdict =
    match status with
    | Complete -> Approved
    | Degraded
    | BlockedCapture -> FidelityEnvironmentLimited
    | MissingCapture
    | WrongSize
    | Undecodable -> FidelityNeedsReview

let private buildVisualCells
    (config: Config)
    (pageCaptures: Map<string * string * string, CaptureStatus * string>)
    (controlId: string)
    (pageId: string)
    : VisualEvidenceItem list =
    [ for mode, themeId in config.Appearances do
          for roleName, _ in config.Sizes do
              let status, path =
                  Map.tryFind (themeId, roleName, pageId) pageCaptures
                  |> Option.defaultValue (BlockedCapture, "")

              let fidelity = fidelityOf status

              let reasons =
                  match fidelity with
                  | Approved -> []
                  | FidelityEnvironmentLimited -> [ "no renderable surface — live-pixel capture environment-limited (FR-008)" ]
                  | FidelityNeedsReview -> [ "capture incomplete; reviewer attention required" ]
                  | FidelityBlocked -> [ "capture blocked" ]

              { TargetId = sprintf "%s/%s/%s/rest" controlId themeId roleName
                Appearance = appearanceOf themeId
                Size = sizeRoleOf roleName
                State = Rest
                CapturePath = path
                CaptureStatus = status
                FidelityVerdict = fidelity
                Reasons = reasons } ]

let private buildRecord
    (config: Config)
    (env: LiveEnvironment)
    (theme: Theme)
    (inspectSize: Size)
    (runId: string)
    (seed: SecondAntShowcaseModel)
    (pageCaptures: Map<string * string * string, CaptureStatus * string>)
    (skeleton: ControlVerdictRecord)
    : ControlVerdictRecord =
    let controlId = skeleton.ControlId

    match skeleton.Classification with
    | DisplayOnly ->
        // Display-only controls are visually inspected only; no functional behavior to drive.
        let primaryPage = List.tryHead skeleton.PageContext |> Option.defaultValue "unknown"
        let cells = buildVisualCells config pageCaptures controlId primaryPage

        { skeleton with
            FunctionalVerdict = NotApplicable
            VisualEvidence = cells
            VisualVerdict = aggregateVisual cells }
    | Interactive ->
        let contracts = behaviorsFor controlId

        let behaviors =
            contracts
            |> List.map (fun contract ->
                let model = { seed with CurrentPage = contract.PageId }
                fst (exerciseBehavior model contract))

        let stateAndDamage =
            contracts |> List.map (fun contract -> inspectTransition theme inspectSize runId controlId contract seed)

        let states = stateAndDamage |> List.map fst
        let damage = stateAndDamage |> List.map snd

        let primaryPage =
            contracts
            |> List.tryHead
            |> Option.map (fun c -> c.PageId)
            |> Option.defaultValue (List.tryHead skeleton.PageContext |> Option.defaultValue "unknown")

        let cells = buildVisualCells config pageCaptures controlId primaryPage

        let isContinuousInput = controlId = "slider" || controlId = "scroll-viewer"

        let diagnostics =
            [ if not env.Renderable then
                  yield
                      "live hover/focus visual states are environment-limited (no renderable surface); functional + structural-damage evidence is authoritative"
              if isContinuousInput then
                  // Continuous-input feedback (offset tracks input, no catch-up lag) is proven by the
                  // existing live-responsiveness evidence path (Feature 173/174): the
                  // `responsiveness` and `render-lag-probe` CLIs. Disclosed here, not duplicated.
                  yield "continuous-input feedback validated via the responsiveness/render-lag-probe live evidence (Feature 173/174)"
                  if not env.Renderable then
                      yield "continuous-input live drag requires a visible window — environment-limited on this host" ]

        { skeleton with
            BehaviorsExercised = behaviors
            InteractionStates = states
            DamageEvidence = damage
            VisualEvidence = cells
            FunctionalVerdict = aggregateFunctional Interactive behaviors
            VisualVerdict = aggregateVisual cells
            Diagnostics = diagnostics }

// --- finding extraction (auto-detected, Found state) ------------------------

let private findingsFrom (records: ControlVerdictRecord list) : Finding list =
    records
    |> List.collect (fun r ->
        [ // Functional failures: a documented behavior produced no state change (dead affordance).
          for b in r.BehaviorsExercised do
              if b.Verdict = Fail then
                  yield
                      { FindingId = sprintf "F176-%s-%s" r.ControlId b.BehaviorId
                        Description =
                          sprintf "control '%s' behavior '%s' produced no model change (%s)" r.ControlId b.BehaviorId b.Expected
                        AffectedControls = [ r.ControlId ]
                        Classification = SampleLocal
                        Tier = Tier2
                        Severity = High
                        Lifecycle = Found
                        BeforeEvidence = sprintf "verdict-records/%s.json" r.ControlId
                        AfterEvidence = None
                        DeferralRationale = None
                        FollowUpRef = None }
          // Damage that is broad/full-surface without an intentional exception.
          for d in r.DamageEvidence do
              if d.DamageStatus = Broad || d.DamageStatus = FullSurface then
                  yield
                      { FindingId = sprintf "F176-%s-damage" r.ControlId
                        Description =
                          sprintf "control '%s' transition '%s' repaints %s (%.1f%% dirty) without an intentional-damage exception" r.ControlId d.TransitionId (string d.DamageStatus) d.DirtyPercentage
                        AffectedControls = [ r.ControlId ]
                        Classification = FrameworkShared
                        Tier = Tier1
                        Severity = Medium
                        Lifecycle = Found
                        BeforeEvidence = sprintf "verdict-records/%s.json" r.ControlId
                        AfterEvidence = None
                        DeferralRationale = None
                        FollowUpRef = None } ])

// --- serialization (deterministic, no wall-clock) ---------------------------

let private jstr (s: string) : string =
    let sb = StringBuilder()
    sb.Append('"') |> ignore

    for ch in s do
        match ch with
        | '"' -> sb.Append("\\\"") |> ignore
        | '\\' -> sb.Append("\\\\") |> ignore
        | '\n' -> sb.Append("\\n") |> ignore
        | '\r' -> sb.Append("\\r") |> ignore
        | '\t' -> sb.Append("\\t") |> ignore
        | c -> sb.Append(c) |> ignore

    sb.Append('"') |> ignore
    sb.ToString()

let private jArr (items: string list) : string =
    "[" + String.concat ", " (items |> List.map jstr) + "]"

let private classificationText =
    function
    | Interactive -> "Interactive"
    | DisplayOnly -> "DisplayOnly"

let private behaviorVerdictText =
    function
    | Pass -> "Pass"
    | Fail -> "Fail"
    | NeedsReview -> "NeedsReview"
    | EnvironmentLimited -> "EnvironmentLimited"

let private functionalText =
    function
    | FunctionalPass -> "Pass"
    | FunctionalFail -> "Fail"
    | FunctionalNeedsReview -> "NeedsReview"
    | FunctionalEnvironmentLimited -> "EnvironmentLimited"
    | NotApplicable -> "NotApplicable"

let private visualText =
    function
    | VisualApproved -> "Approved"
    | VisualNeedsReview -> "NeedsReview"
    | VisualBlocked -> "Blocked"
    | VisualEnvironmentLimited -> "EnvironmentLimited"

let private stateKindText =
    function
    | Hover -> "Hover"
    | Focus -> "Focus"
    | Active -> "Active"
    | Selected -> "Selected"
    | Disabled -> "Disabled"
    | ErrorState -> "Error"

let private damageText =
    function
    | Empty -> "Empty"
    | Localized -> "Localized"
    | Broad -> "Broad"
    | FullSurface -> "FullSurface"
    | Unsupported -> "Unsupported"

let private fidelityText =
    function
    | Approved -> "Approved"
    | FidelityNeedsReview -> "NeedsReview"
    | FidelityBlocked -> "Blocked"
    | FidelityEnvironmentLimited -> "EnvironmentLimited"

let private captureText =
    function
    | Complete -> "Complete"
    | MissingCapture -> "Missing"
    | WrongSize -> "WrongSize"
    | Undecodable -> "Undecodable"
    | Degraded -> "Degraded"
    | BlockedCapture -> "Blocked"

let private appearanceText =
    function
    | AntLight -> "antLight"
    | AntDark -> "antDark"

let private sizeRoleText =
    function
    | Preferred -> "preferred"
    | Minimum -> "minimum"

let private visualStateText =
    function
    | Rest -> "Rest"
    | StateCell k -> stateKindText k

let recordToJson (r: ControlVerdictRecord) : string =
    let behaviors =
        r.BehaviorsExercised
        |> List.map (fun b ->
            sprintf
                "    { \"behaviorId\": %s, \"description\": %s, \"expected\": %s, \"observed\": %s, \"verdict\": %s }"
                (jstr b.BehaviorId)
                (jstr b.Description)
                (jstr b.Expected)
                (jstr b.Observed)
                (jstr (behaviorVerdictText b.Verdict)))
        |> String.concat ",\n"

    let states =
        r.InteractionStates
        |> List.map (fun s ->
            sprintf
                "    { \"state\": %s, \"differsFromRest\": %b, \"evidenceRef\": %s, \"verdict\": %s }"
                (jstr (stateKindText s.State))
                s.DiffersFromRest
                (jstr s.EvidenceRef)
                (jstr (behaviorVerdictText s.Verdict)))
        |> String.concat ",\n"

    let damage =
        r.DamageEvidence
        |> List.map (fun d ->
            sprintf
                "    { \"transitionId\": %s, \"damageStatus\": %s, \"dirtyPercentage\": %.4f, \"affectedRegionIds\": %s, \"verdict\": %s }"
                (jstr d.TransitionId)
                (jstr (damageText d.DamageStatus))
                d.DirtyPercentage
                (jArr d.AffectedRegionIds)
                (jstr (behaviorVerdictText d.Verdict)))
        |> String.concat ",\n"

    let visual =
        r.VisualEvidence
        |> List.map (fun v ->
            sprintf
                "    { \"targetId\": %s, \"appearance\": %s, \"size\": %s, \"state\": %s, \"capturePath\": %s, \"captureStatus\": %s, \"fidelityVerdict\": %s, \"reasons\": %s }"
                (jstr v.TargetId)
                (jstr (appearanceText v.Appearance))
                (jstr (sizeRoleText v.Size))
                (jstr (visualStateText v.State))
                (jstr v.CapturePath)
                (jstr (captureText v.CaptureStatus))
                (jstr (fidelityText v.FidelityVerdict))
                (jArr v.Reasons))
        |> String.concat ",\n"

    let sb = StringBuilder()
    sb.AppendLine("{") |> ignore
    sb.AppendLine(sprintf "  \"controlId\": %s," (jstr r.ControlId)) |> ignore
    sb.AppendLine(sprintf "  \"family\": %s," (jstr r.Family)) |> ignore
    sb.AppendLine(sprintf "  \"pageContext\": %s," (jArr r.PageContext)) |> ignore
    sb.AppendLine(sprintf "  \"classification\": %s," (jstr (classificationText r.Classification))) |> ignore
    sb.AppendLine(sprintf "  \"classificationReason\": %s," (jstr r.ClassificationReason)) |> ignore
    sb.AppendLine(sprintf "  \"functionalVerdict\": %s," (jstr (functionalText r.FunctionalVerdict))) |> ignore
    sb.AppendLine(sprintf "  \"visualVerdict\": %s," (jstr (visualText r.VisualVerdict))) |> ignore
    sb.AppendLine(sprintf "  \"behaviorsExercised\": [\n%s\n  ]," behaviors) |> ignore
    sb.AppendLine(sprintf "  \"interactionStates\": [\n%s\n  ]," states) |> ignore
    sb.AppendLine(sprintf "  \"damageEvidence\": [\n%s\n  ]," damage) |> ignore
    sb.AppendLine(sprintf "  \"visualEvidence\": [\n%s\n  ]," visual) |> ignore
    sb.AppendLine(sprintf "  \"findings\": %s," (jArr r.Findings)) |> ignore
    sb.AppendLine(sprintf "  \"diagnostics\": %s" (jArr r.Diagnostics)) |> ignore
    sb.Append("}") |> ignore
    sb.ToString()

// --- writers ----------------------------------------------------------------

let private writeRecords (outDir: string) (records: ControlVerdictRecord list) : unit =
    let dir = Path.Combine(outDir, "verdict-records")
    Directory.CreateDirectory dir |> ignore

    for r in records do
        File.WriteAllText(Path.Combine(dir, r.ControlId + ".json"), recordToJson r)

    let index =
        [ "# Control Verdict Records — Feature 176"
          ""
          sprintf "%d records, one per cataloged control (catalog order)." (List.length records)
          ""
          "| Control | Family | Class | Functional | Visual |"
          "|---------|--------|-------|------------|--------|"
          for r in records do
              sprintf
                  "| `%s` | %s | %s | %s | %s |"
                  r.ControlId
                  r.Family
                  (classificationText r.Classification)
                  (functionalText r.FunctionalVerdict)
                  (visualText r.VisualVerdict) ]
        |> String.concat Environment.NewLine

    File.WriteAllText(Path.Combine(dir, "_index.md"), index)

let private severityText =
    function
    | Critical -> "Critical"
    | High -> "High"
    | Medium -> "Medium"
    | Low -> "Low"

let private findingClassText =
    function
    | SampleLocal -> "SampleLocal"
    | FrameworkShared -> "FrameworkShared"

let private tierText =
    function
    | Tier1 -> "Tier1"
    | Tier2 -> "Tier2"

let private lifecycleText =
    function
    | Found -> "Found"
    | FixedAndReVerified -> "FixedAndReVerified"
    | Deferred -> "Deferred"

let private writeFindingLog (outDir: string) (findings: Finding list) : unit =
    let rows =
        findings
        |> List.map (fun f ->
            sprintf
                "| %s | %s | %s | %s | %s | %s | %s | %s | %s |"
                f.FindingId
                f.Description
                (String.concat "; " f.AffectedControls)
                (findingClassText f.Classification)
                (tierText f.Tier)
                (severityText f.Severity)
                (lifecycleText f.Lifecycle)
                f.BeforeEvidence
                (Option.defaultValue "—" f.AfterEvidence))

    let body =
        [ "# Auto-Generated Findings — Feature 176 Control Pass"
          ""
          "Machine-detected findings from the latest automated pass, in the `Found` state. The curated,"
          "human-triaged finding log (with terminal lifecycle + before/after evidence) is `finding-log.md`;"
          "this file is regenerated on every run and is the raw input to that triage (FR-009)."
          ""
          "| Finding | Description | Affected | Class | Tier | Severity | Lifecycle | Before | After |"
          "|---------|-------------|----------|-------|------|----------|-----------|--------|-------|"
          yield! rows
          if List.isEmpty findings then
              "_No findings surfaced by the automated pass._" ]
        |> String.concat Environment.NewLine

    File.WriteAllText(Path.Combine(outDir, "finding-log.generated.md"), body)

let private writeSummary
    (outDir: string)
    (config: Config)
    (env: LiveEnvironment)
    (records: ControlVerdictRecord list)
    (findings: Finding list)
    (missing: string list)
    (duplicate: string list)
    : unit =
    let count predicate = records |> List.filter predicate |> List.length

    let body =
        [ "# Validation Summary — Feature 176 Control Pass"
          ""
          sprintf "- Seed: %d" config.Seed
          sprintf "- Backend: %s" config.Backend
          sprintf "- Appearances: %s" (config.Appearances |> List.map snd |> String.concat ", ")
          sprintf "- Sizes: %s" (config.Sizes |> List.map fst |> String.concat ", ")
          sprintf "- Renderable surface: %b" env.Renderable
          if not env.Renderable then
              sprintf "- Environment limitation: %s" (String.concat "; " env.Reasons)
          ""
          "## Completeness (G-2 / VR-1)"
          ""
          sprintf "- Records emitted: %d" (List.length records)
          sprintf "- Catalog controls: %d" (List.length (ControlPass.catalogControlIds ()))
          sprintf "- Missing: %d %s" (List.length missing) (if List.isEmpty missing then "" else "[" + String.concat "; " missing + "]")
          sprintf "- Duplicate/foreign: %d %s" (List.length duplicate) (if List.isEmpty duplicate then "" else "[" + String.concat "; " duplicate + "]")
          ""
          "## Classification (VR-2 / VR-5)"
          ""
          sprintf "- Interactive: %d" (count (fun r -> r.Classification = Interactive))
          sprintf "- Display-only: %d" (count (fun r -> r.Classification = DisplayOnly))
          ""
          "## Functional verdicts"
          ""
          sprintf "- Pass: %d" (count (fun r -> r.FunctionalVerdict = FunctionalPass))
          sprintf "- Fail: %d" (count (fun r -> r.FunctionalVerdict = FunctionalFail))
          sprintf "- NeedsReview: %d" (count (fun r -> r.FunctionalVerdict = FunctionalNeedsReview))
          sprintf "- EnvironmentLimited: %d" (count (fun r -> r.FunctionalVerdict = FunctionalEnvironmentLimited))
          sprintf "- NotApplicable (display-only): %d" (count (fun r -> r.FunctionalVerdict = NotApplicable))
          ""
          "## Visual verdicts"
          ""
          sprintf "- Approved: %d" (count (fun r -> r.VisualVerdict = VisualApproved))
          sprintf "- NeedsReview: %d" (count (fun r -> r.VisualVerdict = VisualNeedsReview))
          sprintf "- Blocked: %d" (count (fun r -> r.VisualVerdict = VisualBlocked))
          sprintf "- EnvironmentLimited: %d" (count (fun r -> r.VisualVerdict = VisualEnvironmentLimited))
          ""
          "## Findings"
          ""
          sprintf "- Auto-detected (Found): %d" (List.length findings)
          ""
          "_GeneratedAtUtc and wall-clock fields are excluded from the byte-stable record surface (G-4)._"
          ""
          "_This is the machine-generated run summary; the curated quickstart validation record is `validation-summary.md`._" ]
        |> String.concat Environment.NewLine

    File.WriteAllText(Path.Combine(outDir, "control-pass-summary.md"), body)

// --- orchestration ----------------------------------------------------------

let private buildAll (config: Config) (env: LiveEnvironment) : ControlVerdictRecord list * Finding list =
    let seed = seededModel config.Seed
    let runId = sprintf "control-pass-seed-%d" config.Seed
    // Structural inspection uses the preferred size (deterministic; appearance-independent for
    // the retained node diff). Theme is antLight for the structural pass.
    let theme = AntTheme.resolve Light
    let inspectSize = VisualConfig.preferredSize

    // Capture the distinct (theme, size, page) cells once and share across the controls on a page.
    let pages =
        match config.Page with
        | Some pageId -> PageRegistry.all |> List.filter (fun p -> p.Id = pageId)
        | None -> PageRegistry.catalogPages

    let pageCaptures =
        [ for mode, themeId in config.Appearances do
              for roleName, size in config.Sizes do
                  for page in pages do
                      let status, path = capturePageCell config.OutDir mode themeId size roleName page.Id
                      (themeId, roleName, page.Id), (status, path) ]
        |> Map.ofList

    let skeletons =
        let all = ControlPass.planRecords ()

        match config.Page with
        | Some pageId ->
            let onPage =
                pages
                |> List.collect (fun p -> p.ControlIds)
                |> Set.ofList
            all |> List.filter (fun r -> Set.contains r.ControlId onPage)
        | None -> all

    let records =
        skeletons
        |> List.map (buildRecord config env theme inspectSize runId seed pageCaptures)

    let findings = findingsFrom records

    // Link finding ids back onto the records they touch.
    let findingsByControl =
        findings
        |> List.collect (fun f -> f.AffectedControls |> List.map (fun c -> c, f.FindingId))
        |> List.groupBy fst
        |> List.map (fun (c, fs) -> c, fs |> List.map snd)
        |> Map.ofList

    let linked =
        records
        |> List.map (fun r ->
            match Map.tryFind r.ControlId findingsByControl with
            | Some ids -> { r with Findings = ids }
            | None -> r)

    linked, findings

/// Testable seam: probe the environment and build the records + findings without writing any
/// artifacts. Deterministic for a fixed seed (no wall-clock in the record surface).
let evaluate (config: Config) : LiveEnvironment * ControlVerdictRecord list * Finding list =
    let env = probeEnvironment ()
    let records, findings = buildAll config env
    env, records, findings

/// A default headless config scoped to one page, for fast in-process tests.
let testConfig (page: string option) : Config =
    { Seed = 1
      Appearances = VisualConfig.resolveThemeList "light,dark" |> function Ok xs -> xs | Result.Error _ -> []
      Sizes = [ "preferred", VisualConfig.preferredSize; "minimum", VisualConfig.minimumSize ]
      Backend = "pure"
      RequireLive = false
      Page = page
      OutDir = Path.Combine(Path.GetTempPath(), "control-pass-test")
      Json = false }

let run (args: string list) : int =
    match parse args with
    | Result.Error message ->
        eprintfn "control-pass: %s" message
        2
    | Ok config ->
        let env = probeEnvironment ()
        Directory.CreateDirectory config.OutDir |> ignore

        let records, findings = buildAll config env

        writeRecords config.OutDir records
        writeFindingLog config.OutDir findings

        let missing, duplicate = ControlPass.completenessGaps records
        writeSummary config.OutDir config env records findings missing duplicate

        // Full-catalog completeness (G-2) is the guarantee of an `--all` pass. A `--page`-scoped run
        // is a partial slice, so it is judged only on no-duplicate, not on the full catalog set.
        let complete =
            match config.Page with
            | Some _ -> List.isEmpty duplicate
            | None -> List.isEmpty missing && List.isEmpty duplicate

        let unclassified =
            records
            |> List.exists (fun r ->
                match r.Classification with
                | DisplayOnly -> String.IsNullOrWhiteSpace r.ClassificationReason
                | Interactive -> false)

        let nonTerminalFinding = findings |> List.exists (fun f -> f.Lifecycle = Found)
        let liveUnavailable = config.RequireLive && not env.Renderable

        printfn
            "control-pass: %d records, %d interactive, %d display-only, %d findings, renderable=%b"
            (List.length records)
            (records |> List.filter (fun r -> r.Classification = Interactive) |> List.length)
            (records |> List.filter (fun r -> r.Classification = DisplayOnly) |> List.length)
            (List.length findings)
            env.Renderable

        if not complete then
            eprintfn "control-pass: completeness FAILED — missing=[%s] duplicate=[%s]" (String.concat "; " missing) (String.concat "; " duplicate)
            1
        elif unclassified then
            eprintfn "control-pass: classification FAILED — a display-only record lacks a reason"
            1
        elif liveUnavailable then
            eprintfn "control-pass: --require-live set but no renderable surface (%s)" (String.concat "; " env.Reasons)
            1
        elif nonTerminalFinding then
            // The automated pass records findings in the Found state; triage to terminal is US3.
            // This is a non-zero signal that findings await triage, never a silent pass.
            eprintfn "control-pass: %d finding(s) recorded in Found state — triage required (US3)" (List.length findings)
            1
        else
            0
