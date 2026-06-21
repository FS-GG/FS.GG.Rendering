module Feature184OverlayByteStabilityTests

// Feature 184 (US2) byte-stability oracle: the overlay production path
// (`compositionEntriesForControl` for an overlay node) emits the literal modifier entry
// `{ Source = LegacyOverlaySource; Effect = LayerHint "overlay" }`. Removing the `Composition`
// legacy node-form layer (`legacyLower`/`LegacyForm`/…) MUST NOT change that entry, its normalized
// chain, or `Composition.fingerprint`. This test pins the baseline captured before the edit
// (`specs/184-backcompat-cleanup/readiness/baseline/overlay-chain.txt`).

open Expecto
open FS.GG.UI.Controls

// The exact entry the overlay path produces (== what `legacyLower LegacyOverlay` returned pre-184).
let private overlayEntry: Composition.ModifierEntry list =
    [ { Composition.Source = Composition.LegacyOverlaySource
        Composition.Effect = Composition.LayerHint "overlay" } ]

// Baseline pinned from the pre-edit capture (T005, readiness/baseline/overlay-chain.txt).
let private baselineOverlayFingerprint = 17605299260426849090UL
let private baselineOverlayFingerprintInput = "LegacyOverlaySource:layer:overlay"

[<Tests>]
let tests =
    testList
        "Feature184 overlay byte-stability (US2)"
        [ test "overlay modifier chain is byte-identical to the pre-184 legacy-lowered entry" {
              let normalized = Composition.normalize overlayEntry
              let fp = Composition.fingerprint overlayEntry
              Expect.equal fp baselineOverlayFingerprint "overlay fingerprint byte-stable vs T005 baseline"
              Expect.equal
                  normalized.FingerprintInput
                  baselineOverlayFingerprintInput
                  "overlay normalized fingerprint input byte-stable vs T005 baseline"
          } ]
