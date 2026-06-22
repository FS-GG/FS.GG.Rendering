module FeatureCatalogTests

open Expecto
open Rendering.Harness
open Rendering.Harness.FeatureCatalog

let private ids = [ 148; 149; 152; 153; 154; 155; 156; 157; 158; 159; 160; 161 ]

[<Tests>]
let tests =
    testList "FeatureCatalog shape" [
        test "catalog has exactly the 12 HEAD features in declaration order (C-FD-1)" {
            Expect.equal (catalog |> List.map (fun d -> d.Id)) ids "catalog ids match Compositor.fs declaration order"
            Expect.equal (List.length catalog) 12 "catalog count is 12"
        }

        test "ValidationSummary and CompatibilityLedger are universal (C-FD-3)" {
            for d in catalog do
                Expect.isTrue (FeatureDescriptor.supports ValidationSummary d) (sprintf "feature %d supports ValidationSummary" d.Id)
                Expect.isTrue (FeatureDescriptor.supports CompatibilityLedger d) (sprintf "feature %d supports CompatibilityLedger" d.Id)
        }

        test "slug uniqueness and id-prefix invariant" {
            Expect.equal (catalog |> List.map (fun d -> d.Slug) |> List.distinct |> List.length) 12 "slugs are unique"
            for d in catalog do
                Expect.isTrue (d.Slug.StartsWith(string d.Id + "-")) (sprintf "slug %s begins with %d-" d.Slug d.Id)
        }

        test "tryByAlias resolves NNN / featureNNN / slug to the right descriptor (C-FD-4)" {
            let f158 = FeatureDescriptor.tryByAlias "158"
            Expect.equal (f158 |> Option.map (fun d -> d.Id)) (Some 158) "\"158\" resolves to 158"
            Expect.equal (FeatureDescriptor.tryByAlias "feature158" |> Option.map (fun d -> d.Id)) (Some 158) "\"feature158\" resolves to 158"
            Expect.equal (FeatureDescriptor.tryByAlias "158-separate-proof-timing" |> Option.map (fun d -> d.Id)) (Some 158) "slug resolves to 158"
        }

        test "tryByAlias is case-insensitive on the slug (C-FD-4)" {
            Expect.equal (FeatureDescriptor.tryByAlias "159-LAYER-PROMOTION-KEYS" |> Option.map (fun d -> d.Id)) (Some 159) "slug match is OrdinalIgnoreCase"
        }

        test "tryByAlias returns None for a feature not in the catalog (legacy fall-through, C-CT-4)" {
            Expect.isNone (FeatureDescriptor.tryByAlias "147") "147 is not in the catalog"
            Expect.isNone (FeatureDescriptor.tryByAlias "999") "unknown feature resolves to None"
        }

        test "derived paths reproduce the hand-declared Compositor constants byte-for-byte (C-FD-2)" {
            let byId id = catalog |> List.find (fun d -> d.Id = id)
            let d148 = byId 148
            // The hand-declared `Compositor.feature148ReadinessDirectory` constant was removed in
            // feature 185 (US1) — the descriptor helper is now the single source. Assert the exact
            // prior byte-string directly (FR-010: equivalent coverage, not weakened).
            Expect.equal (FeatureDescriptor.readinessDirectory d148) (System.IO.Path.Combine("specs", Compositor.Config.feature148Id, "readiness")) "148 readiness dir"
            Expect.equal (FeatureDescriptor.variantDirectory LiveProof d148) Compositor.Config.feature148LiveProofDirectory "148 live-proof dir"
            Expect.equal (FeatureDescriptor.variantDirectory Parity d148) Compositor.Config.feature148ParityDirectory "148 parity dir"
            Expect.equal (FeatureDescriptor.variantDirectory Reuse d148) Compositor.Config.feature148ReuseDirectory "148 reuse dir"
            Expect.equal (FeatureDescriptor.variantDirectory Snapshot d148) Compositor.Config.feature148SnapshotsDirectory "148 snapshots dir"
            Expect.equal (FeatureDescriptor.variantDirectory Timing d148) Compositor.Config.feature148TimingDirectory "148 timing dir"
            Expect.equal (FeatureDescriptor.compatibilityLedgerPath d148) Compositor.Config.feature148CompatibilityLedgerPath "148 ledger path"
            Expect.equal (FeatureDescriptor.validationSummaryPath d148) Compositor.Config.feature148ValidationSummaryPath "148 summary path"
            let d156 = byId 156
            Expect.equal (FeatureDescriptor.packageValidationPath d156) Compositor.Config.feature156PackageValidationPath "156 package-validation path"
            Expect.equal (FeatureDescriptor.regressionValidationPath d156) Compositor.Config.feature156RegressionValidationPath "156 regression-validation path"
        }

        test "config carries shared policy/profile scalars where they exist" {
            let byId id = catalog |> List.find (fun d -> d.Id = id)
            Expect.equal ((byId 156).Config.PolicyId) (Some Compositor.Config.feature156PolicyId) "156 policy id"
            Expect.equal ((byId 158).Config.PolicyId) (Some Compositor.Config.feature158PolicyId) "158 policy id"
            Expect.equal ((byId 156).Config.AcceptedProfileId) (Some Compositor.Config.feature156AcceptedProfileId) "156 accepted profile id"
        }
    ]
