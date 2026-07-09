namespace FS.GG.UI.Elmish

open System
open Elmish

/// Per-frame elapsed delta routed into the author's `update`. Embed it in your
/// own message type (via the `toMsg` mapper) or pattern-match it directly.
type AnimationTick = AnimationTick of TimeSpan

/// Public contract module exposed by this FS.GG.UI package.
///
/// Additive Elmish tick helper (feature 073). The only interpreter-edge
/// component of the animation slice: it advances time by emitting frame-delta
/// messages, and it gates redraws by emitting **only while at least one
/// animation is active**. Wire it through `Program.withSubscription`.
module Animation =
    /// Build the animation tick subscription for the current model. While
    /// `isAnimating model` holds, the returned `Sub` carries a single ticking
    /// entry that dispatches `toMsg interval` (an immediate first frame, then
    /// one per `interval`); once the model settles it returns `Sub.none`, so the
    /// host stops requesting frames (redraw gating at the framework-request
    /// level — FR-006). The `'model -> Sub<'msg>` shape plugs directly into
    /// `Program.withSubscription`.
    ///
    /// **Threading (issue #180).** The first frame dispatches synchronously on
    /// the caller's thread; every later tick dispatches from a
    /// `System.Threading.Timer` callback — a threadpool thread. A host whose
    /// update/render loop is thread-affine must marshal that dispatch onto its
    /// loop thread. `GlHost.run` does (see its `LoopDispatch` gate); a custom
    /// host must, or it will mutate its model and drive its graphics context
    /// off-thread.
    val tickSubscription:
        isAnimating: ('model -> bool) ->
        toMsg: (TimeSpan -> 'msg) ->
        interval: TimeSpan ->
        model: 'model ->
            Sub<'msg>
