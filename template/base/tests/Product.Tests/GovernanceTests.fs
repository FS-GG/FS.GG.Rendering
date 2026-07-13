module AppRootGovernanceTests

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

let approotSource file =
    System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product", file))

let approotSources files =
    files |> List.map approotSource |> String.concat "\n"

let buildScript () =
    System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "build.fsx"))

// Issue #111 (TD1 *Bulwark* feedback §3.2, §4.2): the compile-order scan must anchor to the
// `<Compile Include="X.fs" />` item form, NOT a bare `IndexOf` over the whole project text. A
// bare substring finds "View.fs" inside an additive `BulwarkView.fs`, and even inside a code
// comment mentioning `View.fs` — so scaffolding an extra file (or a comment) silently broke the
// "model compiles before view" gate. The `Include="…"` anchor's opening quote binds the match to
// the start of the filename, so `BulwarkView.fs` / a `// see View.fs` comment no longer match.
let compileIncludeIndex (projectText: string) (file: string) =
    projectText.IndexOf($"Compile Include=\"{file}\"", StringComparison.Ordinal)

// Feature 242 (spec 242-scaffold-discoverability, roadmap #75): a generated product ships a
// SWAP-CHECKLIST.md at its root — the precise model-swap to-do list (§2.2). This durable scan
// asserts PRESENCE + STRUCTURE only (the file names it points at + the scaffold-map pointer), NOT
// the exact per-symbol prose, so a legitimate model swap may rewrite the checklist freely without
// failing the gate (FR-005). The per-symbol accuracy is a template-authoring gate
// (tests/Package.Tests/SwapChecklistTemplateTests.fs), not a product scan.
let approotRootFile file =
    System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", file))

let assertSwapChecklistPresent () =
    let checklist = approotRootFile "SWAP-CHECKLIST.md"

    for anchor in [ "LayoutEvidence.fs"; "EvidenceCommands.fs"; "Model.fs"; "View.fs"; "scaffold-map.md" ] do
        Expect.stringContains checklist anchor $"SWAP-CHECKLIST.md points the developer at {anchor}"

// Feature 242 (§2.3): the build-target help banner surfaces the load-bearing Dev/Test/Verify
// semantics at the build entry. This durable scan asserts build.fsx carries the help branch + the
// semantic phrases and is side-effect-free. fsi reserves --help/-h on the script path, so the
// script trigger is the bare `help` token (T004 live check). The banner↔docs/product.md SYNC
// (FR-009/SC-004) is a template-authoring gate (tests/Package.Tests/SwapChecklistTemplateTests.fs)
// rather than a product scan — that gate reads the template docs verbatim, whereas here a generated
// tree rewrites the `product` token, so it cannot honestly read docs/product.md by name.
let assertBuildHelpBanner () =
    let build = buildScript ()

    Expect.stringContains build "\"help\"" "build.fsx recognizes the bare `help` token (fsi reserves --help/-h)"
    Expect.isFalse (build.Contains("writeLog \"help\"", System.StringComparison.OrdinalIgnoreCase)) "help path is side-effect-free (writes no completion-marker log)"

    for phrase in [ "completion-marker"; "does not compile"; "first real"; "merge-gate audit"; "hard-block" ] do
        Expect.stringContains build phrase $"build.fsx help banner states: {phrase}"

// Feature 202 (US2, FR-002 / SC-004): the generated build.fsx must carry NO pre-rebrand engine
// identifier — neither the `fs.skia.ui.build` NuGet cache folder nor the `FS.Skia.UI` package name —
// so the corrected `fs.gg.ui.build` cache probe cannot silently regress. Scoped to the engine
// package identity + cache path; the deliberately-retained `FsGgUiVersion` property is unaffected.
let assertNoPreRebrandEngineIdentifier () =
    let build = buildScript ()

    Expect.isFalse
        (build.Contains("fs.skia.ui.build", StringComparison.OrdinalIgnoreCase))
        "build.fsx carries no pre-rebrand fs.skia.ui.build cache path"

    Expect.isFalse
        (build.Contains("FS.Skia.UI", StringComparison.OrdinalIgnoreCase))
        "build.fsx carries no pre-rebrand FS.Skia.UI package name"

//#if (profile == "governed" || profile == "headless-scene")
[<Tests>]
let governanceTests =
    testList "product-governance" [
        test "generated headless product exposes deterministic scene evidence command" {
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "--scene-evidence" "headless profile exposes scene evidence"
            Expect.stringContains source "SceneEvidence.render" "scene evidence uses public Scene evidence helper"
            Expect.stringContains source "RendererMode = \"deterministic-scene\"" "scene evidence is deterministic"
            Expect.isFalse (source.Contains("Viewer.runApp")) "headless profile does not require the viewer runtime"
            Expect.isFalse (source.Contains("ControlsElmish")) "headless profile does not require Controls Elmish adapters"
        }

        test "generated build.fsx carries no pre-rebrand engine identifier" {
            assertNoPreRebrandEngineIdentifier ()
        }

        test "generated product ships the SWAP-CHECKLIST discoverability doc" {
            assertSwapChecklistPresent ()
        }

        test "generated build.fsx surfaces the build-target help banner" {
            assertBuildHelpBanner ()
        }
    ]
//#else
[<Tests>]
let governanceTests =
    testList "product-governance" [
        test "generated product source is split by responsibility in compile order" {
            let approotDir = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product")
            let project = System.IO.File.ReadAllText(System.IO.Path.Combine(approotDir, "Product.fsproj"))

            [ "Model.fs"; "View.fs"; "LayoutEvidence.fs"; "WindowOptions.fs"; "EvidenceCommands.fs"; "Program.fs" ]
            |> List.iter (fun file ->
                Expect.isTrue (System.IO.File.Exists(System.IO.Path.Combine(approotDir, file))) $"{file} exists in generated product source"
                Expect.stringContains project $"Compile Include=\"{file}\"" $"{file} is included in compile order")

            let modelIndex = compileIncludeIndex project "Model.fs"
            let viewIndex = compileIncludeIndex project "View.fs"
            let layoutIndex = compileIncludeIndex project "LayoutEvidence.fs"
            let windowOptionsIndex = compileIncludeIndex project "WindowOptions.fs"
            let evidenceIndex = compileIncludeIndex project "EvidenceCommands.fs"
            let programIndex = compileIncludeIndex project "Program.fs"

            Expect.isLessThan modelIndex viewIndex "model compiles before view"
            Expect.isLessThan viewIndex layoutIndex "view compiles before layout evidence"
            Expect.isLessThan layoutIndex windowOptionsIndex "layout evidence compiles before window options"
            Expect.isLessThan windowOptionsIndex evidenceIndex "window options compile before evidence commands"
            Expect.isLessThan evidenceIndex programIndex "evidence commands compile before entrypoint"

            let program = System.IO.File.ReadAllText(System.IO.Path.Combine(approotDir, "Program.fs"))
            Expect.stringContains program "[<EntryPoint>]" "Program.fs keeps the entrypoint"
            Expect.stringContains program "tryRunEvidenceCommand (List.ofArray args)" "Program.fs delegates explicit evidence command dispatch"
            Expect.isFalse (program.Contains("let writeGeneratedEvidenceLines", StringComparison.Ordinal)) "Program.fs does not own report writing"
            Expect.isFalse (program.Contains("let layoutEvidenceForSize size model : LayoutEvidenceReport", StringComparison.Ordinal)) "Program.fs does not own layout evidence implementation"
        }

        test "compile-order scan anchors to Compile Include and survives additive filenames and comments" {
            // Regression for issue #111 (TD1 *Bulwark* feedback §3.2, §4.2): an additive
            // `BulwarkView.fs` and a `// see View.fs` code comment both carry the scanned
            // substring "View.fs" earlier in the project text than "Model.fs" — a bare
            // `IndexOf` scan is fooled into failing "model compiles before view", even though
            // the six load-bearing files keep their relative compile order.
            let synthetic =
                String.concat "\n" [
                    "<Project>"
                    "  <ItemGroup>"
                    "    <!-- additive render helper; see View.fs for the real view -->"
                    "    <Compile Include=\"BulwarkView.fs\" />"
                    "    <Compile Include=\"Model.fs\" />"
                    "    <Compile Include=\"View.fs\" />"
                    "    <Compile Include=\"LayoutEvidence.fs\" />"
                    "  </ItemGroup>"
                    "</Project>"
                ]

            // The anchored scan holds the true compile order despite the decoys.
            Expect.isLessThan (compileIncludeIndex synthetic "Model.fs") (compileIncludeIndex synthetic "View.fs") "anchored scan keeps model before view despite an additive BulwarkView.fs and a `View.fs` comment appearing earlier"
            Expect.isLessThan (compileIncludeIndex synthetic "View.fs") (compileIncludeIndex synthetic "LayoutEvidence.fs") "anchored scan keeps view before layout evidence"

            // Guard the guard: the old bare-substring scan IS fooled here (view before model),
            // so this test would regress the moment the anchor is dropped.
            Expect.isLessThan (synthetic.IndexOf("View.fs", StringComparison.Ordinal)) (synthetic.IndexOf("Model.fs", StringComparison.Ordinal)) "the naive bare-substring scan is demonstrably fooled — the Include anchor is load-bearing"
        }

        test "generated graphical app exposes bounded smoke command" {
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

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
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]
            let program = approotSource "Program.fs"
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
            // persistent interactive host — controls → runInteractiveAppWithAudio, game/sample-pack →
            // runAppWithAudio. #436: both carry the audio sink; no family launches through a
            // sink-discarding overload any more.
            //#if (profile == "app")
            Expect.stringContains defaultBranch "ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost" "controls-family normal launch is the pointer-aware persistent interactive host, with the #429/#436 audio sink"
            //#else
            Expect.stringContains defaultBranch "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" "game/sample-pack normal launch remains the keyboard-only persistent interactive path (with the #245 audio sink)"
            //#endif
            Expect.isFalse (defaultBranch.Contains("mode=persistent-evidence")) "normal launch does not report bounded evidence mode"
            Expect.isFalse (defaultBranch.Contains("self-closed-for-evidence=true")) "normal launch does not claim evidence self-close"
            Expect.isFalse (defaultBranch.Contains("input-dispatch=not-required")) "normal launch does not reuse bounded evidence input-dispatch wording"
            Expect.isFalse (defaultBranch.Contains("--image-evidence")) "image evidence stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--screenshot-evidence")) "screenshot evidence stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--pixel-readback-evidence")) "pixel-readback evidence stays out of normal launch branch"
        }

        test "generated visual evidence commands require screenshot proof pixel fallback and unsupported diagnostics" {
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

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
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

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
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

            Expect.stringContains source "let viewerOptions" "generated product declares viewer options"
            Expect.stringContains source "let generatedHost" "generated product declares generated host"
            Expect.stringContains source "MapKey = mapKey" "generated host wires keyboard mapping"
            Expect.stringContains source "Tick = tick" "generated host wires tick mapping"
            // FR-005 (086): default path runs the per-family persistent interactive host.
            // #436: with the audio sink, on every family.
            //#if (profile == "app")
            Expect.stringContains source "ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost" "controls-family default path runs the pointer-aware persistent host, with the #429/#436 audio sink"
            //#else
            Expect.stringContains source "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" "game/sample-pack default path runs the keyboard-only persistent generated app host (with the #245 audio sink)"
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
            let source = approotSource "Program.fs"
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

        // #136 (child of epic #134): the window-diagnostics probe reported a fabricated
        // status=failed / visible=observed:false window failure REGARDLESS of the real environment,
        // telling the reporter a live window was impossible while `Viewer.runApp` actually launched a
        // visible one. This scan previously LOCKED IN that bug by asserting the source contained the
        // fabricated `observed:*` window facts; it now asserts the truthful, single-source-of-truth
        // shape — the probe derives its verdict from the same gate the real launch consults and never
        // fabricates an observed window failure. (Mirrors #135's flip of its bug-locking assertions.)
        test "generated window diagnostics command derives its verdict from the real launch gate, not fabricated failures" {
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]
            let evidence = approotSource "EvidenceCommands.fs"
            let program = approotSource "Program.fs"
            let defaultBranch = program.Substring(program.LastIndexOf("| None ->", StringComparison.Ordinal))

            Expect.stringContains source "--window-diagnostics" "generated product exposes an explicit window diagnostics command"
            Expect.stringContains source "diagnostic-class=environment-session" "diagnostics include environment/session class"
            Expect.stringContains source "diagnostic-class=window-visibility" "diagnostics include window visibility class"
            Expect.stringContains source "diagnostic-class=app-lifecycle" "diagnostics include app lifecycle class"
            Expect.stringContains source "diagnostic-class=product-defect" "diagnostics include product defect class"
            Expect.stringContains source "native-handle=" "diagnostics still enumerate the native-handle fact"
            Expect.stringContains source "renderable-surface=" "diagnostics still enumerate the renderable-surface fact"
            Expect.stringContains source "input-devices=" "diagnostics still enumerate the input-device fact"
            Expect.stringContains source "fallback-is-full-desktop-session=" "diagnostics disclose fallback session status"

            // Single source of truth: the probe reads the same runtime gate the real launch consults.
            Expect.stringContains evidence "Viewer.runtimeCapability()" "window diagnostics derive their verdict from the same gate the real launch consults"
            Expect.stringContains evidence "Viewer.desktopSessionDiagnostic()" "window diagnostics reflect the real desktop-session determination"
            Expect.stringContains evidence "persistent-window-supported=" "window diagnostics report the host's live-window capability from that gate"

            // The fabricated observed-failure vocabulary is gone: the probe must never claim it SAW a
            // window failure it never opened a window to observe.
            Expect.isFalse (evidence.Contains "visible=observed:false") "window diagnostics no longer fabricate an observed window-invisibility (#136)"
            Expect.isFalse (evidence.Contains "taskbar-only window has no accessible visible surface") "window diagnostics no longer fabricate a taskbar-only visibility failure (#136)"
            Expect.isFalse (evidence.Contains "app lifecycle failed after visible window diagnostics") "window diagnostics no longer fabricate an app-lifecycle failure (#136)"
            Expect.isFalse (evidence.Contains "product requested a zero-sized or surface-less window") "window diagnostics no longer fabricate a product-defect failure (#136)"

            Expect.isFalse (defaultBranch.Contains("--window-diagnostics")) "normal launch does not silently switch to diagnostics mode"
        }

        test "generated app Synthetic exposes window behavior flags and option diagnostics without leaving interactive launch" {
            let source = approotSources [ "Program.fs"; "WindowOptions.fs" ]
            let program = approotSource "Program.fs"
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
            // appropriate to the product family (controls → runInteractiveAppWithAudio,
            // game/sample-pack → runAppWithAudio). #436: both carry the audio sink.
            //#if (profile == "app")
            Expect.stringContains source "ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost" "controls-family default launch applies the pointer-aware persistent viewer contract, with the #429/#436 audio sink"
            //#else
            Expect.stringContains source "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" "game/sample-pack default launch applies the keyboard-only persistent viewer contract (with the #245 audio sink)"
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
            let source = approotSources [ "Program.fs"; "EvidenceCommands.fs" ]

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
            // GeneratedRunner; build.fsx resolves the engine from <FsGgUiVersion> at runtime
            // (no version literal) and delegates the two evidence targets to it by reflection.
            Expect.stringContains build "runGeneratedEvidence \"EvidenceGraph\"" "build delegates the graph command to the engine runner"
            Expect.stringContains build "runGeneratedEvidence \"EvidenceAudit\"" "build delegates the audit command to the engine runner"
            Expect.stringContains build "GeneratedRunner" "build invokes the engine's generated-evidence runner by reflection"
            Expect.stringContains build "Assembly.LoadFrom" "build binds the property-resolved engine assembly at runtime"
            Expect.stringContains build "FsGgUiVersion" "build resolves the engine from the single-source version property"
            // No engine version literal (single-source, FR-004).
            Expect.isFalse
                (Text.RegularExpressions.Regex.IsMatch(build, "#r\\s+\"nuget:\\s*FS\\.Skia\\.UI\\.Build\\s*,"))
                "build carries no literal engine #r version"
            Expect.isFalse (build.Contains("| \"EvidenceGraph\"\n    | \"EvidenceAudit\" -> writeLog target")) "evidence commands are not completion-only logs"
        }

        test "generated build.fsx carries no pre-rebrand engine identifier" {
            assertNoPreRebrandEngineIdentifier ()
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

        test "generated product ships the SWAP-CHECKLIST discoverability doc" {
            assertSwapChecklistPresent ()
        }

        test "generated build.fsx surfaces the build-target help banner" {
            assertBuildHelpBanner ()
        }
    ]
//#endif
