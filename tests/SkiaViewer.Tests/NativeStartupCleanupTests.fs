module internal FS.GG.UI.NativeStartupCleanupTests

open Expecto
open FS.GG.UI.SkiaViewer.Host

[<Tests>]
let nativeStartupCleanupTests =
    testList "Native startup cleanup" [
        let resourceCategories =
            [ GlResources.GlContext
              GlResources.GlSurface
              GlResources.GrContext
              GlResources.Framebuffer
              GlResources.SkiaSurface
              GlResources.SkiaGpu ]

        test "startup stage inventory covers every owned OpenGL and Skia resource category" {
            let stageResources =
                GlStartup.stages
                |> List.choose _.Resource

            resourceCategories
            |> List.iter (fun category -> Expect.contains stageResources category $"stage inventory contains {category}")

            let ordered = GlStartup.stages |> List.sortBy _.Order
            Expect.equal ordered GlStartup.stages "startup stages are declared in acquisition order"
        }

        test "injected acquisition failures Synthetic release acquired resources once in reverse order" {
            // SYNTHETIC: symbolic handles force every failure stage deterministically; real native smoke path is the live GL launch under readiness/.
            for stage in GlStartup.stages |> List.tail do
                let failure = GlStartup.simulateFailure stage.Name

                Expect.isTrue failure.Synthetic "failure fixture discloses synthetic acquisition"
                Expect.equal failure.ExpectedReleaseOrder failure.ObservedReleaseOrder $"release order is reversed for {stage.Name}"
                Expect.equal failure.DiagnosticStage stage.DiagnosticStage $"diagnostic stage is preserved for {stage.Name}"
                Expect.stringContains failure.DiagnosticCause stage.Name $"cause names failing stage {stage.Name}"

                let releasedIds =
                    failure.ObservedReleaseOrder
                    |> List.countBy id
                    |> List.filter (fun (_, count) -> count <> 1)

                Expect.isEmpty releasedIds $"every acquired category is released once for {stage.Name}"
        }

        test "successful shutdown Synthetic releases all acquired resources once and repeated cleanup is idempotent" {
            // SYNTHETIC: symbolic successful acquisition avoids opening a real GL context; real native smoke path is the live GL launch under readiness/.
            let releases = GlStartup.simulateSuccessfulShutdown ()
            let categories = releases |> List.map _.Category

            Expect.equal categories (resourceCategories |> List.rev) "successful shutdown releases resources in reverse acquisition order"

            let duplicates =
                categories
                |> List.countBy id
                |> List.filter (fun (_, count) -> count <> 1)

            Expect.isEmpty duplicates "repeated cleanup does not double-release any resource"
        }
    ]
