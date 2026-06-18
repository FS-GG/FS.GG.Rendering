module Feature148TimingEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature148 timing evidence" [
        test "timing tiers include damage, placement, replay, and snapshot" {
            Expect.equal Compositor.feature148TimingTiers [ "damage"; "placement"; "replay"; "snapshot" ] "tier order"
        }

        test "timing report includes lower-tier baseline and warmup disclosure" {
            let rendered = Compositor.renderFeature148TimingReport "snapshot"
            Expect.stringContains rendered "Tier: `snapshot`" "tier"
            Expect.stringContains rendered "replay/lower tier" "baseline"
            Expect.stringContains rendered "Warmup frames" "warmup"
            Expect.stringContains rendered "Verdict: limited" "non-overclaim verdict"
        }

        test "replay timing tier is accepted by the formatter" {
            let rendered = Compositor.renderFeature148TimingReport "replay"
            Expect.stringContains rendered "Tier: `replay`" "replay tier"
        }
    ]
