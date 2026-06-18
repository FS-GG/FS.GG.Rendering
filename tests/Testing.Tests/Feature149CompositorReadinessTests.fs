module Feature149CompositorReadinessTests

open Expecto
open FS.GG.UI.Testing

[<Tests>]
let tests =
    testList "Feature149 compositor readiness helpers" [
        test "readiness discovery accepts the Feature149 package file set" {
            let required =
                [ "validation-summary.md"
                  "compatibility-ledger.md"
                  "corpus.md"
                  "live-proof/README.md"
                  "parity/README.md"
                  "reuse/README.md"
                  "snapshots/README.md"
                  "timing/README.md" ]

            let result =
                ReadinessFileDiscovery.validate
                    { ReadinessDirectory = "specs/149-complete-compositor-p7/readiness"
                      RequiredFiles = required
                      ExistingFiles = required }

            Expect.isTrue result.Complete "all Feature149 readiness files are discoverable"
            Expect.isEmpty result.MissingFiles "no missing files"
        }

        test "readiness discovery reports missing live proof files without accepting readiness" {
            let result =
                ReadinessFileDiscovery.validate
                    { ReadinessDirectory = "specs/149-complete-compositor-p7/readiness"
                      RequiredFiles = [ "validation-summary.md"; "live-proof/proof.md" ]
                      ExistingFiles = [ "validation-summary.md" ] }

            Expect.isFalse result.Complete "missing proof blocks complete discovery"
            Expect.contains result.MissingFiles "live-proof/proof.md" "missing proof is named"
        }
    ]
