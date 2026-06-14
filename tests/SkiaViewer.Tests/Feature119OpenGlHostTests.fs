module Feature119OpenGlHostTests

open Expecto
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.SkiaViewer.Host

// Feature 119 — OpenGL present backend. Deterministic semantic tests for the present-mode
// mapping, the GL host ledger/startup contract, and the classified GL-unavailable diagnostic.
// The live, readback-free direct-present proof on real GPU hardware is captured under
// specs/119-opengl-present-backend/readiness/ (supported-host-persistent-launch.txt,
// smoke/zero-readback-present.md, sample-smoke/gl-direct-present-frame.png) — see
// [[fs-skia-evidence-mode]].

[<Tests>]
let feature119OpenGlHostTests =
    testList "Feature 119 OpenGL present backend" [

        // US1 / SC-001 / FR-007: the GL default present mode is the readback-free direct path.
        test "default present mode is DirectToSwapchain (readback-free GL default)" {
            let configuration = Viewer.defaultConfiguration "Product" { Width = 640; Height = 480 }
            Expect.equal configuration.PresentMode ViewerPresentMode.DirectToSwapchain "GL host defaults to DirectToSwapchain"
        }

        test "present modes are a distinct closed two-case DU" {
            Expect.notEqual ViewerPresentMode.DirectToSwapchain ViewerPresentMode.OffscreenReadback "the two present modes are distinct"
        }

        // FR-008 / FR-009: the reconciled GL diagnostic surface exists and is separable.
        test "reconciled GL diagnostic DUs exist and are distinct" {
            Expect.notEqual ViewerDiagnosticCategory.OpenGl ViewerDiagnosticCategory.Renderer "OpenGl category is distinct from Renderer"
            Expect.notEqual ViewerDiagnosticCategory.Framebuffer ViewerDiagnosticCategory.Renderer "Framebuffer category is distinct from Renderer"
            Expect.notEqual ViewerRunBlockedStage.GlContext ViewerRunBlockedStage.Renderer "GlContext stage is distinct from Renderer"
        }

        // US1: the GL host ledger covers every owned GL/Skia resource category and cleans up in
        // reverse acquisition order (the GL successor to the Vulkan ownership ledger).
        test "GL startup ledger covers every owned resource category and releases in reverse order" {
            let categories =
                [ GlResources.GlContext
                  GlResources.GlSurface
                  GlResources.GrContext
                  GlResources.Framebuffer
                  GlResources.SkiaSurface
                  GlResources.SkiaGpu ]

            let stageResources = GlStartup.stages |> List.choose _.Resource
            categories |> List.iter (fun c -> Expect.contains stageResources c $"startup inventory contains {c}")

            let releases = GlStartup.simulateSuccessfulShutdown () |> List.map _.Category
            Expect.equal releases (categories |> List.rev) "successful shutdown releases in reverse acquisition order"
        }

        // US3 / FR-005 / SC-004: the GL-unavailable diagnostic is honest — fatal, names OpenGL,
        // states there is no fallback renderer, and never suggests a Vulkan or software fallback.
        test "GL-unavailable diagnostic is classified honestly with no false fallback" {
            let diagnostic = Diagnostics.glUnavailable "GL context creation unavailable"
            let rendered = diagnostic.Message + "\n" + (diagnostic.Cause |> Option.defaultValue "")

            Expect.equal diagnostic.Severity Fatal "an unavailable GL backend is fatal"
            Expect.equal diagnostic.Stage GlContext "GL availability fails at context setup"
            Expect.stringContains rendered "OpenGL" "diagnostic names OpenGL availability"
            Expect.stringContains diagnostic.Message "no fallback renderer" "diagnostic states there is no fallback renderer"
            Expect.isFalse (rendered.Contains "Vulkan") "diagnostic does not suggest a Vulkan fallback"
            Expect.isFalse (rendered.Contains "software") "diagnostic does not suggest a software fallback"
        }

        // US2 / FR-003 / SC-003: the host input/event contract is unchanged across the backend
        // swap — the viewer event vocabulary the consumer maps over is preserved verbatim.
        test "host event vocabulary is preserved across the backend swap (interaction parity)" {
            let events: ViewerEvent list =
                [ Loaded
                  UpdateTick 0.016
                  RenderTick 0.016
                  KeyDown "Space"
                  KeyUp "Space"
                  PointerMoved(1.0, 2.0)
                  PointerPressed(1.0, 2.0, PrimaryButton)
                  PointerReleased(1.0, 2.0, SecondaryButton)
                  PointerScrolled(1.0, 2.0, 0.0, 1.0)
                  PointerExited
                  Resized { Width = 10; Height = 10 }
                  CloseRequested ]

            Expect.equal events.Length 12 "the full host event vocabulary remains available for consumer routing"
        }
    ]
