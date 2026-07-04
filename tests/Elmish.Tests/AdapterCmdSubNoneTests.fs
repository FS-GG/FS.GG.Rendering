module AdapterCmdSubNoneTests

// specs/241-runtime-ergonomics (FS-GG/FS.GG.Rendering#74, §3.5): the product-facing `Cmd.none` /
// `Sub.none` no-op aliases on `FS.GG.UI.Controls.Elmish.Authoring`. They exist so a product `update`
// returns `model, Cmd.none` instead of a bare `model, []` (Elmish convention). This locks their
// behavioural identity with `[]` — the whole point is that the swap is a no-op.
//
// Named by content (not `Feature241...`) because the internal FeatureNNN test sequence already uses
// 241 for the unrelated activation-value work (PR #53); the speckit feature number is separate.

open Expecto
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Controls.Elmish.Authoring

[<Tests>]
let tests =
    testList
        "adapter no-op aliases (§3.5)"
        [ test "Cmd.none is the empty AdapterCommand" {
              let c: AdapterCommand<int> = Cmd.none
              Expect.equal c [] "Cmd.none must equal []"
          }

          test "Cmd.none carries no product messages" {
              Expect.equal (AdapterCmd.productMessages (Cmd.none: AdapterCommand<int>)) [] "no product messages"
          }

          test "Sub.none is the empty subscription list" {
              let s: AdapterSubscription<int> list = Sub.none
              Expect.isEmpty s "Sub.none must equal []"
          }

          test "returning Cmd.none is identical to returning []" {
              let updateNew (m: int) : int * AdapterCommand<int> = m, Cmd.none
              let updateOld (m: int) : int * AdapterCommand<int> = m, []
              Expect.equal (updateNew 7) (updateOld 7) "Cmd.none swap must be behaviour-preserving" } ]
