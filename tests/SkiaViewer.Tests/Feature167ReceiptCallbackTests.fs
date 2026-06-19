module Feature167ReceiptCallbackTests

open System
open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

[<Tests>]
let tests =
    testList "Feature167 receipt callback" [
        test "receipt diagnostic records enqueue-and-signal without render work" {
            let receipt =
                GlHost.recordInputReceipt
                    1L
                    "pointer-discrete"
                    0
                    (TimeSpan.FromMilliseconds 0.5)
                    true
                    false

            Expect.isTrue receipt.SignalRequested "callback requested a wake/signal"
            Expect.isFalse (GlHost.receiptDidRenderWork receipt) "callback did not start retained render work"
            Expect.isTrue
                (GlHost.receiptWithinBudget
                    Viewer.defaultResponsivenessBudget.InputReceiptP95
                    Viewer.defaultResponsivenessBudget.InputReceiptMax
                    receipt)
                "receipt is within budget"
        }

        test "presentation timing reports missing live boundary explicitly" {
            let timing = GlHost.presentationTiming 42L None None false

            Expect.equal timing.EnvironmentStatus "no-visible-surface" "missing surface is explicit"
            Expect.equal timing.PresentedFrameId 42L "frame id is retained"
        }
    ]
