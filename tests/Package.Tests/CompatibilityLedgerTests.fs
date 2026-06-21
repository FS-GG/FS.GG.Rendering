module CompatibilityLedgerTests

open System.IO
open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host
open FS.GG.UI.Testing
open FS.GG.TestSupport

// Feature 181 (US3): one data-driven testList replacing the 12 per-feature
// Feature###Compatibility*Tests.fs files for the catalog features (148,149,152,153,154 +
// 155-161). Each `Check` reads a generated readiness artifact and asserts feature-specific tokens —
// equivalent coverage to the per-feature files it replaces (FR-005/SC-004). The feature id list
// mirrors FeatureCatalog.catalog (a ProjectReference to the harness exe is intentionally avoided so
// the release-only Package.Tests surface/feed graph is not perturbed — T024 fallback path); the
// catalog-coverage test below locks the mirror.

type private Check = { Name: string; Path: string; Tokens: string list }

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private fileCheck (c: Check) =
    test c.Name {
        let path = repo c.Path
        Expect.isTrue (File.Exists path) $"file exists at {path}"
        let text = File.ReadAllText path
        c.Tokens |> List.iter (fun required -> Expect.stringContains text required required)
    }

// Feature 157 ports two in-memory compatibility tests verbatim (not file-based) to preserve coverage.
let private feature157InMemoryTests =
    [ test "SkiaViewer exposes additive damage decision and diagnostics vocabulary" {
          Expect.equal (Viewer.damageDecisionToken ViewerDamageDecision.FullRedraw) "full-redraw" "viewer damage token"
          let diagnostic = Diagnostics.damageScopedDecision "damage-scoped-accepted" None
          Expect.stringContains diagnostic.Message "Feature157" "diagnostic mentions feature"
      }

      test "Testing exposes Feature157 damage readiness validation without accepting performance" {
          let check =
              { Feature = "157-no-clear-damage-scissor"
                RequiredScenarioIds = [ "damage/static-preserved" ]
                Scenarios =
                  [ { ScenarioId = "damage/static-preserved"
                      Status = CompositorDamageAccepted
                      AcceptedAttemptCount = 3
                      ArtifactPaths = [ "damage/attempts/static.md" ]
                      FallbackReason = None } ]
                AcceptedAttemptCount = 3
                UnsupportedHostStatus = CompositorDamageEnvironmentLimited
                AcceptedPartialRedrawArtifacts = 0
                CompatibilityAccepted = true
                PackageAccepted = true
                RegressionAccepted = true
                PerformanceClaim = "performance-not-accepted"
                Limitations = [] }

          let result = CompositorDamageReadiness.validate check
          Expect.isTrue result.Accepted "damage correctness accepted"
          Expect.equal (CompositorDamageReadiness.statusText result.Status) "accepted" "status token"
      } ]

let private featureChecks: (int * string * Check list) list =
    [ 148, "Feature148 compatibility ledger",
      [ { Name = "compatibility ledger records public metrics, baselines, release notes, migration, and limitations"
          Path = "specs/148-compositor-live-integration/readiness/compatibility-ledger.md"
          Tokens =
            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations" ] }
        { Name = "validation summary lists live proof, damage, placement, replay, and snapshot tier statuses"
          Path = "specs/148-compositor-live-integration/readiness/validation-summary.md"
          Tokens = [ "Live proof"; "Damage scissor"; "Placement reuse"; "Replay"; "Snapshot"; "environment-limited" ] }
        { Name = "corpus names target hosts, replay timing, exact budgets, and synthetic disclosure"
          Path = "specs/148-compositor-live-integration/readiness/corpus.md"
          Tokens =
            [ "synthetic-non-preserving"
              "timing/replay"
              "32 MiB"
              "64 retained snapshot candidates"
              "proof/live-sentinel-damage-v1" ] } ]

      149, "Feature149 compatibility ledger",
      [ { Name = "compatibility ledger records public metrics, baselines, release notes, migration, and limitations"
          Path = "specs/149-complete-compositor-p7/readiness/compatibility-ledger.md"
          Tokens =
            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations"
              "Feature149 harness routes" ] }
        { Name = "validation summary lists live proof, damage, placement, replay, snapshot, timing, and diagnostics"
          Path = "specs/149-complete-compositor-p7/readiness/validation-summary.md"
          Tokens =
            [ "Live proof"; "Damage scissor"; "Placement reuse"; "Replay"; "Snapshot"; "Timing"; "Public diagnostics"; "environment-limited" ] }
        { Name = "corpus names target hosts, timing tiers, budgets, and synthetic disclosure"
          Path = "specs/149-complete-compositor-p7/readiness/corpus.md"
          Tokens =
            [ "feature149-capable-host-candidate"
              "synthetic-non-preserving"
              "timing/replay"
              "32 MiB"
              "64 retained snapshot candidates"
              "proof/live-sentinel-damage-v1" ] } ]

      152, "Feature152 compatibility ledger",
      [ { Name = "compatibility ledger records public proof-set and readiness helper effects"
          Path = "specs/152-compositor-live-proof/readiness/compatibility-ledger.md"
          Tokens = [ "CompositorProof"; "CompositorReadiness"; "full redraw"; "Migration Guidance"; "Synthetic simulations" ] }
        { Name = "validation summary records environment-limited status without performance overclaim"
          Path = "specs/152-compositor-live-proof/readiness/validation-summary.md"
          Tokens =
            [ "Status: `environment-limited`"
              "Performance claim: `environment-limited`"
              "zero accepted partial-redraw artifacts"
              "No compositor performance claim is accepted" ] } ]

      153, "Feature153 compatibility ledger",
      [ { Name = "compatibility ledger records proof interpreter public effects"
          Path = "specs/153-compositor-proof-interpreter/readiness/compatibility-ledger.md"
          Tokens =
            [ "CompositorProof.AcceptedProofSet"
              "GlHost.LiveProofHostFacts"
              "Viewer.liveProofInterpreterSupported"
              "CompositorReadiness"
              "Synthetic Disclosure" ] }
        { Name = "validation summary records environment-limited status without partial-redraw or performance overclaim"
          Path = "specs/153-compositor-proof-interpreter/readiness/validation-summary.md"
          Tokens =
            [ "Status: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "zero accepted partial-redraw artifacts"
              "No compositor performance claim is accepted" ] } ]

      154, "Feature154 compatibility ledger",
      [ { Name = "compatibility ledger records proof readiness diagnostics and public drift decision"
          Path = "specs/154-compositor-proof-acceptance/readiness/compatibility-ledger.md"
          Tokens =
            [ "CompositorProof.AcceptedProofSet"
              "CompositorReadiness"
              "No new public `.fsi` surface is required"
              "Controls and Controls.Elmish compositor diagnostics"
              "Synthetic Disclosure" ] }
        { Name = "validation summary records environment-limited final status without overclaiming"
          Path = "specs/154-compositor-proof-acceptance/readiness/validation-summary.md"
          Tokens =
            [ "Status: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `0/3`"
              "zero accepted partial-redraw artifacts" ] } ]

      155, "Feature155 compatibility closeout",
      [ { Name = "validation summary records accepted correctness without performance overclaim"
          Path = "specs/155-native-proof-capture/readiness/validation-summary.md"
          Tokens =
            [ "Status: `accepted`"
              "Proof set: `accepted`"
              "Parity status: `accepted`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `3/3`" ] }
        { Name = "compatibility ledger scopes Feature155 to current-host P7 closeout"
          Path = "specs/155-native-proof-capture/readiness/compatibility-ledger.md"
          Tokens =
            [ "Feature 155 reuses the Feature 154 proof-set"
              "No new public `.fsi` surface is required"
              "current-host P7 correctness closeout"
              "Performance remains a separate claim" ] } ]

      156, "Feature156 compatibility package",
      [ { Name = "timing summary preserves performance-not-accepted and remaining gates"
          Path = "specs/156-same-profile-timing/readiness/timing/summary.md"
          Tokens =
            [ "Policy id: `same-profile-live-threshold-v2`"
              "Accepted profile id: `probe-08a47c01`"
              "Shipped P7 performance claim: `performance-not-accepted`"
              "Feature 157 damage-scissored no-clear renderer"
              "Feature 160 validation throughput follow-up"
              "Feature 161 host performance lane ledger" ] }
        { Name = "compatibility ledger documents additive public helper surface"
          Path = "specs/156-same-profile-timing/readiness/compatibility-ledger.md"
          Tokens =
            [ "CompositorTimingAssertions"
              "FS.GG.UI.SkiaViewer.CompositorProof"
              "compositor-performance --feature 156"
              "Existing Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative" ] }
        { Name = "package validation records surface and FSI evidence"
          Path = "specs/156-same-profile-timing/readiness/package-validation.md"
          Tokens = [ "SkiaViewer and Testing surface baselines"; "Package FSI transcript coverage"; "compositor-readiness --feature 156" ] } ]

      157, "Feature157 compatibility package",
      [ { Name = "compatibility ledger documents additive public surface and no shipped performance claim"
          Path = "specs/157-no-clear-damage-scissor/readiness/compatibility-ledger.md"
          Tokens = [ "GlHost"; "CompositorDamageReadiness"; "performance-not-accepted" ] }
        { Name = "package validation records Feature157 commands and surface boundary"
          Path = "specs/157-no-clear-damage-scissor/readiness/package-validation.md"
          Tokens = [ "compositor-readiness --feature 157"; "SkiaViewer, Testing, and harness signatures" ] } ]

      158, "Feature158 compatibility package",
      [ { Name = "compatibility ledger documents no new package helper surface"
          Path = "specs/158-separate-proof-timing/readiness/compatibility-ledger.md"
          Tokens =
            [ "No new `FS.GG.UI.Testing` public helper surface"
              "No new `FS.GG.UI.SkiaViewer` public helper surface"
              "performance-not-accepted" ] }
        { Name = "package validation records command surface and FSI evidence"
          Path = "specs/158-separate-proof-timing/readiness/package-validation.md"
          Tokens = [ "compositor-readiness --feature 158"; "No Testing or SkiaViewer package-visible helper surface" ] }
        { Name = "validation summary keeps proof probes separate from performance claim"
          Path = "specs/158-separate-proof-timing/readiness/validation-summary.md"
          Tokens = [ "proof-probes/README.md"; "timing/excluded/"; "performance-not-accepted" ] } ]

      159, "Feature159 compatibility package",
      [ { Name = "compatibility ledger documents package surface decisions and claim boundary"
          Path = "specs/159-layer-promotion-keys/readiness/compatibility-ledger.md"
          Tokens = [ "FS.GG.UI.Controls"; "FS.GG.UI.SkiaViewer"; "Feature159Readiness"; "performance-not-accepted" ] }
        { Name = "package validation records command surface and FSI evidence"
          Path = "specs/159-layer-promotion-keys/readiness/package-validation.md"
          Tokens = [ "compositor-readiness --feature 159"; "Feature159Readiness" ] }
        { Name = "validation summary links promotion counters and unsupported-host evidence"
          Path = "specs/159-layer-promotion-keys/readiness/validation-summary.md"
          Tokens = [ "promotion/summary.md"; "counters/promotion.md"; "promotion/unsupported/validation.md"; "performance-not-accepted" ] } ]

      160, "Feature160 compatibility package",
      [ { Name = "compatibility ledger documents helper surface and claim boundary"
          Path = "specs/160-performance-validation-throughput/readiness/compatibility-ledger.md"
          Tokens = [ "Feature160ThroughputReadiness"; "compositor-performance --feature 160 --lane focused"; "performance-not-accepted" ] }
        { Name = "package validation records command surface and FSI evidence"
          Path = "specs/160-performance-validation-throughput/readiness/package-validation.md"
          Tokens = [ "compositor-readiness --feature 160"; "Feature160ThroughputReadiness" ] }
        { Name = "validation summary links throughput full validation and unsupported-host evidence"
          Path = "specs/160-performance-validation-throughput/readiness/validation-summary.md"
          Tokens = [ "throughput/summary.md"; "full-validation/validation.md"; "throughput/unsupported/README.md"; "performance-not-accepted" ] } ]

      161, "Feature161 compatibility package",
      [ { Name = "compatibility ledger documents helper surface command surface and claim boundary"
          Path = "specs/161-host-performance-lane-ledger/readiness/compatibility-ledger.md"
          Tokens = [ "Feature161HostLaneReadiness"; "compositor-performance --feature 161 --lane host-ledger"; "performance-not-accepted" ] }
        { Name = "package validation records command surface Testing helper and FSI evidence"
          Path = "specs/161-host-performance-lane-ledger/readiness/package-validation.md"
          Tokens =
            [ "compositor-readiness --feature 161"
              "Feature161HostLaneReadiness"
              "compositor-host-lane-authoring.fsx"
              "feature161-host-lane-readiness-authoring.fsx" ] }
        { Name = "validation summary links ledger full validation compatibility package regression and unsupported-host evidence"
          Path = "specs/161-host-performance-lane-ledger/readiness/validation-summary.md"
          Tokens =
            [ "lane-ledger/summary.md"
              "lane-ledger/host-facts/"
              "full-validation/validation.md"
              "lane-ledger/unsupported/README.md"
              "performance-not-accepted" ] }
        { Name = "surface evidence records Feature 161 Testing additive public surface"
          Path = "specs/161-host-performance-lane-ledger/readiness/fsi/FS.GG.UI.Testing.txt"
          Tokens = [ "Feature161HostLaneReadiness" ] }
        { Name = "surface evidence records Feature 161 Compositor harness surface"
          Path = "specs/161-host-performance-lane-ledger/readiness/fsi/Rendering.Harness.Compositor.txt"
          Tokens = [ "Feature 161" ] }
        { Name = "surface evidence records Feature 161 Perf harness surface"
          Path = "specs/161-host-performance-lane-ledger/readiness/fsi/Rendering.Harness.Perf.txt"
          Tokens = [ "missing-display" ] } ] ]

[<Tests>]
let tests =
    testList "Feature compatibility (catalog data-driven)" [
        for (id, name, checks) in featureChecks do
            let fileTests = checks |> List.map fileCheck
            let extra = if id = 157 then feature157InMemoryTests else []
            yield testList name (fileTests @ extra)

        yield test "catalog coverage: compatibility checks cover exactly the FeatureCatalog ids" {
            let covered = featureChecks |> List.map (fun (id, _, _) -> id)
            Expect.equal covered [ 148; 149; 152; 153; 154; 155; 156; 157; 158; 159; 160; 161 ] "covers exactly the catalog ids (mirrors FeatureCatalog.catalog)"
        }
    ]
