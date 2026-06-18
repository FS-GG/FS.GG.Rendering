module Feature149TimingEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature149 timing evidence" [
        test "timing tiers include damage, placement, replay, and snapshot" {
            Expect.equal Compositor.feature149TimingTiers [ "damage"; "placement"; "replay"; "snapshot" ] "tier order"
        }

        test "timing report includes lower-tier baseline and warmup disclosure" {
            let rendered = Compositor.renderFeature149TimingReport "snapshot"
            Expect.stringContains rendered "Tier: `snapshot`" "tier"
            Expect.stringContains rendered "replay/lower tier" "baseline"
            Expect.stringContains rendered "Warmup frames" "warmup"
            Expect.stringContains rendered "Verdict: limited" "non-overclaim verdict"
        }

        test "replay timing tier compares lower-tier and full-frame baselines" {
            let rendered = Compositor.renderFeature149TimingReport "replay"
            Expect.stringContains rendered "Tier: `replay`" "replay tier"
            Expect.stringContains rendered "full-frame oracle" "oracle baseline"
        }
    ]
