module Feature142TextCacheParityTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private empty: TextMeasureCache = { Entries = Map.empty; Clock = 0 }
let private font: FontSpec = { Family = Some "Inter"; Size = 16.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 shaped text cache parity" [
        test "provider bucket changes force a text-cache miss" {
            let _, warm, firstHit = RetainedRender.measureTextCachedWithBucket "bucket-a" empty true "cache me" font
            Expect.isFalse firstHit "cold measurement misses"

            let _, _, sameBucketHit = RetainedRender.measureTextCachedWithBucket "bucket-a" warm true "cache me" font
            Expect.isTrue sameBucketHit "same bucket hits"

            let _, _, changedBucketHit = RetainedRender.measureTextCachedWithBucket "bucket-b" warm true "cache me" font
            Expect.isFalse changedBucketHit "provider bucket participates in the key"
        }
    ]
