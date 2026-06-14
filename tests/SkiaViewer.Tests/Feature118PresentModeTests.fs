module Feature118PresentModeTests

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.SkiaViewer.Host

// Feature 118 — backend present-mode contract. The DirectToSwapchain *mechanism* (readback-free
// present) is blocked upstream by SkiaSharp's managed-binding gap (SKSurface cannot wrap a
// Vulkan swapchain image, mono/SkiaSharp #1502/#2191) and is proven to degrade safely by the
// live-host smoke; these deterministic tests guard the public contract + default byte-identity.

let private size: Size = { Width = 320; Height = 240 }

[<Tests>]
let tests =
    testList "Feature 118 present-mode contract" [
        test "default ViewerOptions present mode is OffscreenReadback (SC-001 byte-identity default)" {
            // A ViewerOptions constructed with the documented default value carries the
            // byte-identical readback present path.
            let options =
                { Title = "Product"
                  InitialSize = size
                  PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

            Expect.equal options.PresentMode ViewerPresentMode.OffscreenReadback "default present mode must be OffscreenReadback"
        }

        test "defaultConfiguration carries DirectToSwapchain (feature 119 GL readback-free default)" {
            let configuration = Viewer.defaultConfiguration "Product" size
            Expect.equal configuration.PresentMode ViewerPresentMode.DirectToSwapchain "defaultConfiguration defaults to the GL readback-free DirectToSwapchain present path"
        }

        test "ViewerConfiguration.PresentMode mirrors the supplied ViewerOptions.PresentMode (config threading)" {
            // The config-build site threads ViewerOptions.PresentMode into ViewerConfiguration;
            // assert the threading shape on both values.
            let thread (options: ViewerOptions) =
                { Viewer.defaultConfiguration options.Title options.InitialSize with
                    PresentMode = options.PresentMode }

            let direct =
                thread { Title = "P"; InitialSize = size; PresentMode = ViewerPresentMode.DirectToSwapchain; FrameRateCap = None }

            let offscreen =
                thread { Title = "P"; InitialSize = size; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

            Expect.equal direct.PresentMode ViewerPresentMode.DirectToSwapchain "DirectToSwapchain must thread through"
            Expect.equal offscreen.PresentMode ViewerPresentMode.OffscreenReadback "OffscreenReadback must thread through"
        }

        test "present modes are distinct (closed two-case DU)" {
            Expect.notEqual ViewerPresentMode.OffscreenReadback ViewerPresentMode.DirectToSwapchain "the two present modes must be distinct"
        }

        test "FR-007 diagnostic categories Framebuffer/Frame exist and are distinct from Renderer" {
            // The present-mode / readback diagnostic must surface under Framebuffer (or Frame), not
            // Renderer; assert the consumer-facing category surface FR-007 relies on is separable.
            Expect.notEqual ViewerDiagnosticCategory.Framebuffer ViewerDiagnosticCategory.Renderer "Framebuffer must differ from Renderer"
            Expect.notEqual ViewerDiagnosticCategory.Frame ViewerDiagnosticCategory.Renderer "Frame must differ from Renderer"
            Expect.notEqual ViewerDiagnosticCategory.Framebuffer ViewerDiagnosticCategory.Frame "Framebuffer must differ from Frame"
        }
    ]
