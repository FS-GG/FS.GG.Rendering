module ProductGovernanceTests

open System
open Expecto

// Feature 060 (FR-005): durable, model-agnostic governance scans. These read the
// generated product SOURCE TEXT (and build.fsx) and assert structural / evidence /
// discoverability invariants that survive a scaffold-model swap. Replace the scaffold
// model freely — only `BehaviorTests.fs` needs rewriting; this file keeps compiling and
// passing because it never calls the product's `view`/`update`.

// Visual-evidence honesty vocabulary asserted by the generated-guidance governance scans
// (kept here so the durable governance file owns the model-agnostic vocabulary).
let visualEvidenceGuidance =
    "decodable image; image dimensions; non-trivial content; renderer mode; fallback classification; unsupported reason; metadata-only reports do not satisfy visual proof; 1x1 fallback images do not satisfy visual proof; layout-only bounds claims do not satisfy visual proof; framework runtime; generated template workflow; documentation discoverability; consumer authoring; persistent-window blocking; display/session availability; auto-close smoke; benign warning; blocking warning; deferred warning; name-collision guidance"

let productSource file =
    System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product", file))

let productSources files =
    files |> List.map productSource |> String.concat "\n"

//#if (profile == "governed" || profile == "headless-scene")
[<Tests>]
let governanceTests =
    testList "product-governance" [
        test "generated headless product exposes deterministic scene evidence command" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "--scene-evidence" "headless profile exposes scene evidence"
            Expect.stringContains source "SceneEvidence.render" "scene evidence uses public Scene evidence helper"
            Expect.stringContains source "RendererMode = \"deterministic-scene\"" "scene evidence is deterministic"
            Expect.isFalse (source.Contains("Viewer.runApp")) "headless profile does not require the viewer runtime"
            Expect.isFalse (source.Contains("ControlsElmish")) "headless profile does not require Controls Elmish adapters"
        }
    ]
//#else
[<Tests>]
let governanceTests =
    testList "product-governance" [
        test "generated product source is split by responsibility in compile order" {
            let productDir = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product")
            let project = System.IO.File.ReadAllText(System.IO.Path.Combine(productDir, "Product.fsproj"))

            [ "Model.fs"; "View.fs"; "LayoutEvidence.fs"; "WindowOptions.fs"; "EvidenceCommands.fs"; "Program.fs" ]
            |> List.iter (fun file ->
                Expect.isTrue (System.IO.File.Exists(System.IO.Path.Combine(productDir, file))) $"{file} exists in generated product source"
                Expect.stringContains project $"Compile Include=\"{file}\"" $"{file} is included in compile order")

            let modelIndex = project.IndexOf("Model.fs", StringComparison.Ordinal)
            let viewIndex = project.IndexOf("View.fs", StringComparison.Ordinal)
            let layoutIndex = project.IndexOf("LayoutEvidence.fs", StringComparison.Ordinal)
            let windowOptionsIndex = project.IndexOf("WindowOptions.fs", StringComparison.Ordinal)
            let evidenceIndex = project.IndexOf("EvidenceCommands.fs", StringComparison.Ordinal)
            let programIndex = project.IndexOf("Program.fs", StringComparison.Ordinal)

            Expect.isLessThan modelIndex viewIndex "model compiles before view"
            Expect.isLessThan viewIndex layoutIndex "view compiles before layout evidence"
            Expect.isLessThan layoutIndex windowOptionsIndex "layout evidence compiles before window options"
            Expect.isLessThan windowOptionsIndex evidenceIndex "window options compile before evidence commands"
            Expect.isLessThan evidenceIndex programIndex "evidence commands compile before entrypoint"

            let program = System.IO.File.ReadAllText(System.IO.Path.Combine(productDir, "Program.fs"))
            Expect.stringContains program "[<EntryPoint>]" "Program.fs keeps the entrypoint"
            Expect.stringContains program "tryRunEvidenceCommand (List.ofArray args)" "Program.fs delegates explicit evidence command dispatch"
            Expect.isFalse (program.Contains("let writeGeneratedEvidenceLines", StringComparison.Ordinal)) "Program.fs does not own report writing"
            Expect.isFalse (program.Contains("let layoutEvidenceForSize size model : LayoutEvidenceReport", StringComparison.Ordinal)) "Program.fs does not own layout evidence implementation"
        }

        test "generated graphical app exposes bounded smoke command" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "--launch-evidence" "generated product exposes explicit launch evidence CLI"
            Expect.stringContains source "Viewer.runBounded" "launch evidence uses a bounded evidence entry point"
            Expect.stringContains source "mode=persistent-evidence" "launch evidence reports evidence mode"
            Expect.stringContains source "--bounded-smoke" "generated product exposes bounded smoke CLI"
            Expect.stringContains source "--bounded-smoke-frame-diagnostics" "generated product exposes explicit frame diagnostic smoke CLI"
            Expect.stringContains source "Viewer.runBounded" "bounded smoke uses the public SkiaViewer bounded run entry point"
            Expect.stringContains source "status=unsupported" "bounded smoke reports unsupported host conditions explicitly"
            Expect.stringContains source "diagnostic-mode={diagnosticMode}" "generated smoke writes readable diagnostics mode"
            Expect.stringContains source "startup-focused" "startup-focused generated smoke is the default"
            Expect.stringContains source "frame-focused" "frame-focused generated smoke is opt-in"
            Expect.stringContains source "FrameLogLimit = if includeFrameDiagnostics then Some 1 else Some 0" "generated smoke limits repeated frame diagnostics"
        }

        test "generated evidence commands are opt-in and not reported as ongoing interactive play" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]
            let program = productSource "Program.fs"
            let defaultBranch = program.Substring(program.LastIndexOf("| None ->", StringComparison.Ordinal))

            Expect.stringContains source "--launch-evidence" "first-frame launch evidence is exposed only by explicit CLI flag"
            Expect.stringContains source "--bounded-smoke" "bounded evidence smoke is exposed only by explicit CLI flag"
            Expect.stringContains source "--bounded-smoke-frame-diagnostics" "frame diagnostics are exposed only by explicit CLI flag"
            Expect.stringContains source "--image-evidence" "image evidence is exposed only by explicit CLI flag"
            Expect.stringContains source "--screenshot-evidence" "screenshot evidence is exposed only by explicit CLI flag"
            Expect.stringContains source "--pixel-readback-evidence" "pixel-readback evidence is exposed only by explicit CLI flag"
            Expect.stringContains source "input-dispatch=not-required" "bounded evidence reports that input dispatch is not an interactive-play claim"
            Expect.stringContains source "self-closed-for-evidence=true" "bounded evidence reports self-close semantics"
            Expect.stringContains source "mode=persistent-evidence" "bounded evidence uses persistent evidence mode"
            Expect.stringContains source "command=--launch-evidence" "first-frame evidence records the evidence command"
            Expect.stringContains source "\"--image-evidence\"" "image evidence records the evidence command"
            Expect.stringContains source "\"--screenshot-evidence\"" "screenshot evidence records the evidence command"
            Expect.stringContains source "\"--pixel-readback-evidence\"" "pixel-readback evidence records the evidence command"
            Expect.stringContains source "Viewer.runBounded" "generated evidence commands use bounded viewer evidence entry points"
            // FR-005 (086, D6): the host-lock assertion is generalized to the per-family
            // persistent interactive host — controls → runInteractiveApp, game → runApp.
            //#if (profile == "app")
            Expect.stringContains defaultBranch "ControlsElmish.runInteractiveApp viewerOptions interactiveHost" "controls-family normal launch is the pointer-aware persistent interactive host"
            //#else
            Expect.stringContains defaultBranch "Viewer.runApp viewerOptions generatedHost" "game-family normal launch remains the keyboard-only persistent interactive path"
            //#endif
            Expect.isFalse (defaultBranch.Contains("mode=persistent-evidence")) "normal launch does not report bounded evidence mode"
            Expect.isFalse (defaultBranch.Contains("self-closed-for-evidence=true")) "normal launch does not claim evidence self-close"
            Expect.isFalse (defaultBranch.Contains("input-dispatch=not-required")) "normal launch does not reuse bounded evidence input-dispatch wording"
            Expect.isFalse (defaultBranch.Contains("--image-evidence")) "image evidence stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--screenshot-evidence")) "screenshot evidence stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--pixel-readback-evidence")) "pixel-readback evidence stays out of normal launch branch"
        }

        test "generated visual evidence commands require screenshot proof pixel fallback and unsupported diagnostics" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "--image-evidence" "generated product exposes image evidence command"
            Expect.stringContains source "--screenshot-evidence" "generated product exposes screenshot evidence command"
            Expect.stringContains source "--pixel-readback-evidence" "generated product exposes pixel-readback evidence command"
            Expect.stringContains source "evidenceField \"evidence-kind\" \"image\"" "image command records image evidence kind"
            Expect.stringContains source "evidenceField \"image-decodable\"" "image command records decodability"
            Expect.stringContains source "evidenceField \"proves-scene-rendering\" \"true\"" "image command records scene-rendering proof claim"
            Expect.stringContains source "evidenceField \"proves-desktop-visibility\" \"false\"" "image command records desktop-visibility proof claim"
            Expect.stringContains source "evidenceField \"evidence-kind\" \"screenshot\"" "screenshot command records screenshot evidence kind"
            Expect.stringContains source "Viewer.captureScreenshotEvidence" "screenshot command uses the viewer screenshot evidence contract"
            Expect.stringContains source "deterministic-scene-evidence" "unsupported screenshot command records deterministic fallback"
            Expect.stringContains source "evidenceField \"viewer-open-status\"" "screenshot command reports viewer-open status"
            Expect.stringContains source "evidenceField \"first-frame-status\"" "screenshot command reports first-frame status"
            Expect.stringContains source "evidenceField \"capture-availability\"" "screenshot command reports capture availability"
            Expect.stringContains source "evidenceField \"capture-source\"" "screenshot command reports capture source"
            Expect.stringContains source "evidenceField \"deterministic-fallback-kind\"" "screenshot command reports deterministic fallback kind"
            Expect.stringContains source "evidenceField \"proves-screenshot\"" "screenshot command reports screenshot proof boolean"
            Expect.isFalse (source.Contains("evidenceField \"capture-source\" \"pixel-readback\"", StringComparison.Ordinal)) "pixel readback is not relabeled as screenshot capture source"
            Expect.isFalse (source.Contains("evidenceField \"capture-source\" \"deterministic-scene-render\"\n              evidenceField \"proves-screenshot\" \"true\"", StringComparison.Ordinal)) "deterministic render is not relabeled as screenshot proof"
            Expect.stringContains source "evidenceField \"evidence-kind\" evidenceKind" "pixel-readback command records fallback evidence kind"
            Expect.stringContains source "evidenceField \"fallback-reason\" fallbackReason" "pixel-readback command records why screenshot proof was unavailable"
            Expect.stringContains source "screenshot-unavailable" "pixel-readback command names screenshot unavailability"
            Expect.stringContains source "evidenceField \"playfield-readable\" \"true\"" "visual evidence proves the playfield/grid is readable"
            Expect.stringContains source "evidenceField \"input-or-progress-observed\" \"true\"" "visual evidence proves input dispatch or time progression was observed"
            Expect.stringContains source "evidenceField \"unsupported-host-reason\"" "unsupported visual evidence reports why neither visual path is available"
            Expect.stringContains source "evidenceField \"supported-host\" \"false\"" "unsupported visual evidence is explicit instead of substituting text-only metadata"
        }

        test "generated evidence commands share Testing report conventions" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "let writeEvidenceReport" "generated product defines one local report wrapper"
            Expect.stringContains source "generatedEvidenceStatusText" "generated product shares normalized report status vocabulary"
            Expect.stringContains source "| GeneratedEvidenceOk -> \"ok\"" "generated product preserves ok status vocabulary"
            Expect.stringContains source "| GeneratedEvidenceUnsupported -> \"unsupported\"" "generated product preserves unsupported status vocabulary"
            Expect.stringContains source "| GeneratedEvidenceFailed -> \"failed\"" "generated product preserves failed status vocabulary"
            Expect.stringContains source "generatedEvidenceExitCode" "generated product keeps report status to exit-code semantics local"
            Expect.stringContains source "| GeneratedEvidenceUnsupported -> 0" "unsupported generated evidence remains a non-failing host fact"
            Expect.stringContains source "| GeneratedEvidenceFailed -> 1" "failed generated evidence remains a failing command result"
            Expect.stringContains source "writeEvidenceReport" "shared report wrapper is called by generated evidence commands"
            Expect.stringContains source "evidenceField \"command\" command" "report wrapper preserves command field"
            Expect.stringContains source "evidenceField \"output\" evidencePath" "report wrapper preserves output field"
            Expect.stringContains source "writeGeneratedEvidenceLines evidencePath true (generatedEvidenceExitCode status) lines" "report wrapper creates parent directories, writes the requested output path, and preserves exit-code semantics"
            Expect.stringContains source "lines |> List.iter (printfn \"%s\")" "report wrapper echoes report fields to stdout"
            Expect.stringContains source "\"--layout-evidence\"" "layout command reports through the shared convention"
            Expect.stringContains source "\"--launch-evidence\"" "launch command preserves its public command name"
            Expect.stringContains source "\"--image-evidence\"" "image command reports through the shared convention"
            Expect.stringContains source "\"--screenshot-evidence\"" "screenshot command reports through the shared convention"
            Expect.stringContains source "\"--pixel-readback-evidence\"" "pixel-readback command reports through the shared convention"
        }

        test "generated graphical app default executable path uses persistent host" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "let viewerOptions" "generated product declares viewer options"
            Expect.stringContains source "let generatedHost" "generated product declares generated host"
            Expect.stringContains source "MapKey = mapKey" "generated host wires keyboard mapping"
            Expect.stringContains source "Tick = tick" "generated host wires tick mapping"
            // FR-005 (086): default path runs the per-family persistent interactive host.
            //#if (profile == "app")
            Expect.stringContains source "ControlsElmish.runInteractiveApp viewerOptions interactiveHost" "controls-family default path runs the pointer-aware persistent host"
            //#else
            Expect.stringContains source "Viewer.runApp viewerOptions generatedHost" "game-family default path runs the keyboard-only persistent generated app host"
            //#endif
            Expect.stringContains source "mode=interactive-window" "default path reports interactive mode"
            Expect.stringContains source "accessible-window=true" "successful default path reports accessible desktop window claim"
            Expect.stringContains source "window-visible=observed:true" "successful default path reports observed visible window"
            Expect.stringContains source "accessible-window=false" "unsupported default path does not claim visible accessibility"
            Expect.stringContains source "mode=interactive-window" "unsupported default diagnostics still identify interactive mode"
            Expect.stringContains source "--bounded-smoke" "bounded smoke remains behind an explicit flag"
            Expect.stringContains source "--launch-evidence" "launch evidence remains behind an explicit flag"
        }

        test "generated normal launch reports desktop session diagnostics without evidence fallback" {
            let source = productSource "Program.fs"
            let defaultBranch = source.Substring(source.LastIndexOf("| None ->", StringComparison.Ordinal))

            Expect.stringContains defaultBranch "Viewer.desktopSessionDiagnostic()" "normal launch captures desktop/session diagnostics before app lifecycle debugging"
            Expect.stringContains defaultBranch "diagnostic-class=" "normal launch reports diagnostic classification"
            Expect.stringContains defaultBranch "runtime-directory=" "normal launch reports runtime directory state"
            Expect.stringContains defaultBranch "display-variable=" "normal launch reports display variable state"
            Expect.stringContains defaultBranch "display-socket-exists=" "normal launch reports display socket state"
            Expect.stringContains defaultBranch "session-bus=" "normal launch reports session bus state"
            Expect.stringContains defaultBranch "fallback-is-full-desktop-session=false" "private runtime fallback is labeled as not a full desktop session"
            Expect.isFalse (defaultBranch.Contains("Viewer.runBounded")) "normal launch does not silently switch to bounded evidence"
            Expect.isFalse (defaultBranch.Contains("SceneEvidence.render")) "normal launch does not silently switch to scene-only metadata"
            Expect.isFalse (defaultBranch.Contains("--launch-evidence")) "explicit evidence flag stays out of normal launch diagnostics"
            Expect.isFalse (defaultBranch.Contains("--scene-evidence")) "scene evidence flag stays out of normal launch diagnostics"
        }

        test "generated window diagnostics command reports failure classes and native facts before app debugging" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]
            let program = productSource "Program.fs"
            let defaultBranch = program.Substring(program.LastIndexOf("| None ->", StringComparison.Ordinal))

            Expect.stringContains source "--window-diagnostics" "generated product exposes an explicit window diagnostics command"
            Expect.stringContains source "diagnostic-class=environment-session" "diagnostics include environment/session class"
            Expect.stringContains source "diagnostic-class=window-visibility" "diagnostics include window visibility class"
            Expect.stringContains source "diagnostic-class=app-lifecycle" "diagnostics include app lifecycle class"
            Expect.stringContains source "diagnostic-class=product-defect" "diagnostics include product defect class"
            Expect.stringContains source "native-handle=observed:true" "diagnostics include native handle facts"
            Expect.stringContains source "visible=observed:false" "diagnostics include visible observed-false facts"
            Expect.stringContains source "focusable=observed:false" "diagnostics include focusable facts"
            Expect.stringContains source "minimized=observed:false" "diagnostics include minimized facts"
            Expect.stringContains source "maximized=observed:false" "diagnostics include maximized facts"
            Expect.stringContains source "client-size=0x0" "diagnostics include zero-sized client facts"
            Expect.stringContains source "renderable-surface=observed:false" "diagnostics include renderable-surface facts"
            Expect.stringContains source "input-devices=unavailable" "diagnostics include input-device availability facts"
            Expect.stringContains source "fallback-is-full-desktop-session=" "diagnostics disclose fallback session status"
            Expect.isFalse (defaultBranch.Contains("--window-diagnostics")) "normal launch does not silently switch to diagnostics mode"
        }

        test "generated app Synthetic exposes window behavior flags and option diagnostics without leaving interactive launch" {
            let source = productSources [ "Program.fs"; "WindowOptions.fs" ]
            let program = productSource "Program.fs"
            let defaultBranch = program.Substring(program.LastIndexOf("| None ->", StringComparison.Ordinal))

            Expect.stringContains source "--window-resize" "resize policy is configurable"
            Expect.stringContains source "--window-maximize" "maximize policy is configurable"
            Expect.stringContains source "--window-startup" "startup state is configurable"
            Expect.stringContains source "--window-position" "startup position is configurable"
            Expect.stringContains source "--window-backend" "backend preference is configurable"
            Expect.stringContains source "--window-options-file" "option files are supported"
            Expect.stringContains source "--window-options" "generated product exposes option diagnostics"
            Expect.stringContains source "windowBehaviorArgsFromFile" "option files are parsed into launch flags"
            Expect.stringContains source "toViewerWindowBehavior windowBehavior" "parsed flags become the public viewer request"
            Expect.stringContains source "Viewer.validateWindowLaunchBehavior viewerOptions.InitialSize" "generated diagnostics use public launch behavior validation"
            // FR-005 (086): the default launch applies the selected persistent viewer contract
            // appropriate to the product family (controls → runInteractiveApp, game → runApp).
            //#if (profile == "app")
            Expect.stringContains source "ControlsElmish.runInteractiveApp viewerOptions interactiveHost" "controls-family default launch applies the pointer-aware persistent viewer contract"
            //#else
            Expect.stringContains source "Viewer.runApp viewerOptions generatedHost" "game-family default launch applies the keyboard-only persistent viewer contract"
            //#endif
            Expect.stringContains source "manualWindowOptionResults windowBehaviorRequest" "normal launch validates parsed behavior request before calling SkiaViewer"
            Expect.stringContains source "window-options=%s" "normal launch reports option validation output"
            Expect.stringContains source "option=resize" "option report includes resize rows"
            Expect.stringContains source "option=maximize" "option report includes maximize rows"
            Expect.stringContains source "option=startup-state" "option report includes startup-state rows"
            Expect.stringContains source "option=startup-position" "option report includes startup-position rows"
            Expect.stringContains source "option=backend" "option report includes backend rows"
            Expect.stringContains source "status=unsupported" "unsupported host/backend option diagnostics are explicit"
            Expect.isFalse (defaultBranch.Contains("Viewer.runBounded")) "window options do not switch normal launch to bounded evidence"
        }

        test "generated graphical app exposes deterministic scene evidence command" {
            let source = productSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "--scene-evidence" "generated product exposes non-window scene evidence CLI"
            Expect.stringContains source "SceneEvidence.render" "scene evidence uses public Scene evidence helper"
            Expect.stringContains source "RendererMode = \"deterministic-scene\"" "scene evidence remains separate from live viewer startup"
            Expect.stringContains source "readiness/headless-scene-evidence.txt" "scene evidence writes a stable readiness path"
        }

        test "generated evidence graph command runs the in-process engine" {
            let build = System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "build.fsx"))

            // Feature 043 (FR-013): generated evidence runs in-process through the packaged
            // FS.GG.UI.Build engine — no copied Python / run-audit.sh.
            // Feature 064 (FR-004 / R1): the in-process orchestration lives in the engine's
            // GeneratedRunner; build.fsx resolves the engine from <FsSkiaUiVersion> at runtime
            // (no version literal) and delegates the two evidence targets to it by reflection.
            Expect.stringContains build "runGeneratedEvidence \"EvidenceGraph\"" "build delegates the graph command to the engine runner"
            Expect.stringContains build "runGeneratedEvidence \"EvidenceAudit\"" "build delegates the audit command to the engine runner"
            Expect.stringContains build "GeneratedRunner" "build invokes the engine's generated-evidence runner by reflection"
            Expect.stringContains build "Assembly.LoadFrom" "build binds the property-resolved engine assembly at runtime"
            Expect.stringContains build "FsSkiaUiVersion" "build resolves the engine from the single-source version property"
            // No engine version literal (single-source, FR-004).
            Expect.isFalse
                (Text.RegularExpressions.Regex.IsMatch(build, "#r\\s+\"nuget:\\s*FS\\.Skia\\.UI\\.Build\\s*,"))
                "build carries no literal engine #r version"
            Expect.isFalse (build.Contains("| \"EvidenceGraph\"\n    | \"EvidenceAudit\" -> writeLog target")) "evidence commands are not completion-only logs"
        }

        test "generated evidence graph and audit do not shell the decommissioned scripts" {
            let build = System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "build.fsx"))

            [ "run-audit.sh"; "compute-task-graph.py"; "python3"; "ProcessStartInfo(\"bash\"" ]
            |> List.iter (fun forbidden ->
                Expect.isFalse (build.Contains(forbidden, StringComparison.Ordinal)) $"generated evidence workflow excludes the decommissioned {forbidden}")
            Expect.isFalse (build.Contains("chmod", StringComparison.OrdinalIgnoreCase)) "generated evidence workflow does not repair executable mode"
        }

        test "generated Verify redirected output is clean text" {
            let build = System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "build.fsx"))

            Expect.stringContains build "RedirectStandardOutput <- true" "generated Verify captures stdout as text"
            Expect.stringContains build "RedirectStandardError <- true" "generated Verify captures stderr as text"
            Expect.stringContains build "let output = stdout + stderr" "generated Verify combines text streams"
            Expect.stringContains build "tryWriteTextLog logPath output" "generated Verify writes text logs through the checked text writer"
            Expect.stringContains build "printf \"%s\" output" "generated Verify echoes text without binary padding"

            [ "File.WriteAllBytes"; "BinaryWriter"; "\\u0000"; "Array.zeroCreate" ]
            |> List.iter (fun forbidden ->
                Expect.isFalse (build.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) $"generated Verify excludes binary log writer {forbidden}")
        }
    ]
//#endif
