module Feature175RepaintSignalTests

// Feature 175 F1 — the "presentation path" regression the value-path tests (Feature175NavFocus/Toggle)
// could not reach. The actual repaint happens inside the GL/timing-bound viewer loops, where a frame
// count is nondeterministic (variable startup/resize derivations). So we lock the SHARED decision both
// loops now route through — `Viewer.runtimeStateRepaint` — deterministically: it re-derives the scene
// iff the input produced NO product message (runtime state — focus/hover/scroll — may still have
// changed with no model change), and is a no-op when messages already drove a dispatch+re-derive.
// Both viewer loops (key-only and full-interactive) call this one policy, so locking it locks the
// "focus one click behind / dead-hover / dead-scroll" class for both. (`runtimeStateRepaint` is generic
// over the scene type, so the test uses plain sentinels — the policy, not scene construction, is the SUT.)

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature175RepaintSignal" [
        test "no product message → the scene is re-derived from host.View (renders THIS input)" {
            let derivations = ref 0
            let result = Viewer.runtimeStateRepaint false "stale" (fun () -> incr derivations; "fresh")
            Expect.equal !derivations 1 "a no-message input re-derives the scene exactly once (not left stale)"
            Expect.equal result "fresh" "the re-derived scene is presented"
        }

        test "product messages present → no extra re-derive (dispatch already refreshed)" {
            let derivations = ref 0
            let result = Viewer.runtimeStateRepaint true "already-derived" (fun () -> incr derivations; "fresh")
            Expect.equal !derivations 0 "when messages ran, dispatchHostMsg already re-derived — no redundant View call"
            Expect.equal result "already-derived" "the already-derived scene is kept"
        }
    ]
