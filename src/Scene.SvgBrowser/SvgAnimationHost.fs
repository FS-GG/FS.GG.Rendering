namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core
open FS.GG.UI.Scene

[<RequireQualifiedAccess>]
type SvgMotionPreference =
    | Full
    | Reduced

[<RequireQualifiedAccess>]
type SvgReducedMotionBehavior =
    | Settle
    | Substitute

type SvgAnimationImportance =
    | Essential
    | Decorative of SvgReducedMotionBehavior

type SvgAnimationRequest =
    {
        Id: string
        AuthorityRevision: uint64
        Clip: ValidatedAnimationClip
        Importance: SvgAnimationImportance
    }

type SvgAnimationPolicyConfig = { MaxDecorativeEffects: int }

[<RequireQualifiedAccess>]
type SvgAnimationRefusal =
    | EmptyEffectId
    | DuplicateEffectId of string
    | StaleAuthorityRevision of expected: uint64 * actual: uint64
    | DecorativeLimitReached of limit: int
    | UnknownEffect of string
    | Disposed

[<RequireQualifiedAccess>]
type SvgAnimationStatus =
    | Running
    | Paused
    | Disposed

type private ActiveAnimation =
    {
        Request: SvgAnimationRequest
        Elapsed: TimeSpan
    }

type SvgAnimationPolicyState =
    private
        {
            AuthorityRevision: uint64
            PresentationRevision: uint64
            Status: SvgAnimationStatus
            MotionPreference: SvgMotionPreference
            Active: Map<string, ActiveAnimation>
        }

[<RequireQualifiedAccess>]
type SvgAnimationObservation =
    | Start of SvgAnimationRequest
    | Frame of elapsed: TimeSpan
    | Pause
    | Resume
    | Seek of effectId: string * elapsed: TimeSpan
    | ReplaceAuthority of revision: uint64
    | SetMotionPreference of SvgMotionPreference
    | Dispose

[<RequireQualifiedAccess>]
type SvgAnimationEffect =
    | ApplySample of
        effectId: string *
        authorityRevision: uint64 *
        presentationRevision: uint64 *
        sample: AnimationClipSample
    | CueBatch of effectId: string * authorityRevision: uint64 * cues: CueOccurrence list
    | EffectCompleted of effectId: string
    | EffectRefused of SvgAnimationRefusal
    | ScheduleFrame
    | CancelFrame
    | Disposed

[<RequireQualifiedAccess>]
module SvgAnimationPolicy =
    let private validate config =
        if config.MaxDecorativeEffects < 0 || config.MaxDecorativeEffects > 10000 then
            invalidArg (nameof config) "decorative effect limit must be between zero and 10,000"

    let initialize config authorityRevision motionPreference =
        validate config

        {
            AuthorityRevision = authorityRevision
            PresentationRevision = 0UL
            Status = SvgAnimationStatus.Running
            MotionPreference = motionPreference
            Active = Map.empty
        }

    let observe state =
        state.AuthorityRevision, state.PresentationRevision, state.Status, state.MotionPreference, state.Active.Count

    let private nextRevision state =
        if state.PresentationRevision = UInt64.MaxValue then
            invalidOp "SVG animation presentation revision space is exhausted."

        state.PresentationRevision + 1UL

    let private appendSchedule state effects =
        if state.Status = SvgAnimationStatus.Running && not state.Active.IsEmpty then
            effects @ [ SvgAnimationEffect.ScheduleFrame ]
        else
            effects @ [ SvgAnimationEffect.CancelFrame ]

    let private applyAt elapsed cueMode request state =
        let revision = nextRevision state
        let sample = AnimationClip.sample elapsed cueMode request.Clip

        let effects =
            [
                SvgAnimationEffect.ApplySample(request.Id, request.AuthorityRevision, revision, sample)
                if not sample.Cues.IsEmpty then
                    SvgAnimationEffect.CueBatch(request.Id, request.AuthorityRevision, sample.Cues)
            ]

        { state with
            PresentationRevision = revision
        },
        sample,
        effects

    let private completionElapsed request =
        let clip = AnimationClip.value request.Clip

        let iterations =
            match clip.Loop with
            | Once -> 1
            | Repeat count
            | PingPong count -> count

        TimeSpan.FromMilliseconds(clip.Duration.TotalMilliseconds * float iterations)

    let private safeAdd request (a: TimeSpan) (b: TimeSpan) =
        let completion = completionElapsed request

        if b <= TimeSpan.Zero then a
        elif b >= completion - a then completion
        else a + b

    let update config observation state =
        validate config

        if state.Status = SvgAnimationStatus.Disposed then
            match observation with
            | SvgAnimationObservation.Dispose -> state, []
            | _ -> state, [ SvgAnimationEffect.EffectRefused SvgAnimationRefusal.Disposed ]
        else
            match observation with
            | SvgAnimationObservation.Start request when String.IsNullOrWhiteSpace request.Id ->
                state, [ SvgAnimationEffect.EffectRefused SvgAnimationRefusal.EmptyEffectId ]
            | SvgAnimationObservation.Start request when request.AuthorityRevision <> state.AuthorityRevision ->
                state,
                [
                    SvgAnimationEffect.EffectRefused(
                        SvgAnimationRefusal.StaleAuthorityRevision(state.AuthorityRevision, request.AuthorityRevision)
                    )
                ]
            | SvgAnimationObservation.Start request when state.Active.ContainsKey request.Id ->
                state,
                [
                    SvgAnimationEffect.EffectRefused(SvgAnimationRefusal.DuplicateEffectId request.Id)
                ]
            | SvgAnimationObservation.Start request ->
                let decorativeCount =
                    state.Active
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, active) ->
                        match active.Request.Importance with
                        | Decorative _ -> 1
                        | Essential -> 0)

                match request.Importance, state.MotionPreference with
                | Decorative _, _ when decorativeCount >= config.MaxDecorativeEffects ->
                    state,
                    [
                        SvgAnimationEffect.EffectRefused(
                            SvgAnimationRefusal.DecorativeLimitReached config.MaxDecorativeEffects
                        )
                    ]
                | Decorative behavior, SvgMotionPreference.Reduced ->
                    let elapsed =
                        match behavior with
                        | SvgReducedMotionBehavior.Settle -> completionElapsed request
                        | SvgReducedMotionBehavior.Substitute -> TimeSpan.Zero

                    let sampled, _, effects = applyAt elapsed ClipCueMode.Seek request state

                    sampled,
                    effects
                    @ [
                        SvgAnimationEffect.EffectCompleted request.Id
                        SvgAnimationEffect.CancelFrame
                    ]
                | _ ->
                    let active =
                        {
                            Request = request
                            Elapsed = TimeSpan.Zero
                        }

                    let accepted =
                        { state with
                            Active = state.Active.Add(request.Id, active)
                        }

                    let sampled, _, effects = applyAt TimeSpan.Zero ClipCueMode.Seek request accepted
                    sampled, appendSchedule sampled effects
            | SvgAnimationObservation.Frame elapsed when
                state.Status <> SvgAnimationStatus.Running || elapsed <= TimeSpan.Zero
                ->
                state, []
            | SvgAnimationObservation.Frame elapsed ->
                let mutable current = state
                let mutable nextActive = state.Active
                let effects = ResizeArray<SvgAnimationEffect>()

                for KeyValue(id, active) in state.Active do
                    let nextElapsed = safeAdd active.Request active.Elapsed elapsed

                    let sampled, sample, sampleEffects =
                        applyAt nextElapsed (ClipCueMode.LiveAdvance active.Elapsed) active.Request current

                    current <- sampled
                    sampleEffects |> List.iter effects.Add

                    if sample.IsComplete then
                        nextActive <- nextActive.Remove id
                        effects.Add(SvgAnimationEffect.EffectCompleted id)
                    else
                        nextActive <- nextActive.Add(id, { active with Elapsed = nextElapsed })

                let accepted = { current with Active = nextActive }
                accepted, appendSchedule accepted (List.ofSeq effects)
            | SvgAnimationObservation.Pause when state.Status = SvgAnimationStatus.Running ->
                { state with
                    Status = SvgAnimationStatus.Paused
                },
                [ SvgAnimationEffect.CancelFrame ]
            | SvgAnimationObservation.Pause -> state, []
            | SvgAnimationObservation.Resume when state.Status = SvgAnimationStatus.Paused ->
                let resumed =
                    { state with
                        Status = SvgAnimationStatus.Running
                    }

                resumed, appendSchedule resumed []
            | SvgAnimationObservation.Resume -> state, []
            | SvgAnimationObservation.Seek(id, _) when not (state.Active.ContainsKey id) ->
                state, [ SvgAnimationEffect.EffectRefused(SvgAnimationRefusal.UnknownEffect id) ]
            | SvgAnimationObservation.Seek(id, elapsed) ->
                let active = state.Active[id]
                let bounded = if elapsed < TimeSpan.Zero then TimeSpan.Zero else elapsed
                let sampled, sample, effects = applyAt bounded ClipCueMode.Seek active.Request state

                let nextActive =
                    if sample.IsComplete then
                        sampled.Active.Remove id
                    else
                        sampled.Active.Add(id, { active with Elapsed = bounded })

                let accepted = { sampled with Active = nextActive }

                let completed =
                    if sample.IsComplete then
                        effects @ [ SvgAnimationEffect.EffectCompleted id ]
                    else
                        effects

                accepted, appendSchedule accepted completed
            | SvgAnimationObservation.ReplaceAuthority revision when revision <= state.AuthorityRevision ->
                state,
                [
                    SvgAnimationEffect.EffectRefused(
                        SvgAnimationRefusal.StaleAuthorityRevision(state.AuthorityRevision, revision)
                    )
                ]
            | SvgAnimationObservation.ReplaceAuthority revision ->
                { state with
                    AuthorityRevision = revision
                    Active = Map.empty
                },
                [ SvgAnimationEffect.CancelFrame ]
            | SvgAnimationObservation.SetMotionPreference preference when preference = state.MotionPreference ->
                state, []
            | SvgAnimationObservation.SetMotionPreference SvgMotionPreference.Full ->
                let accepted =
                    { state with
                        MotionPreference = SvgMotionPreference.Full
                    }

                accepted, appendSchedule accepted []
            | SvgAnimationObservation.SetMotionPreference SvgMotionPreference.Reduced ->
                let mutable current =
                    { state with
                        MotionPreference = SvgMotionPreference.Reduced
                    }

                let mutable nextActive = state.Active
                let effects = ResizeArray<SvgAnimationEffect>()

                for KeyValue(id, active) in state.Active do
                    match active.Request.Importance with
                    | Essential -> ()
                    | Decorative behavior ->
                        let elapsed =
                            match behavior with
                            | SvgReducedMotionBehavior.Settle -> completionElapsed active.Request
                            | SvgReducedMotionBehavior.Substitute -> TimeSpan.Zero

                        let sampled, _, sampleEffects =
                            applyAt elapsed ClipCueMode.Seek active.Request current

                        current <- sampled
                        sampleEffects |> List.iter effects.Add
                        effects.Add(SvgAnimationEffect.EffectCompleted id)
                        nextActive <- nextActive.Remove id

                let accepted = { current with Active = nextActive }
                accepted, appendSchedule accepted (List.ofSeq effects)
            | SvgAnimationObservation.Dispose ->
                { state with
                    Status = SvgAnimationStatus.Disposed
                    Active = Map.empty
                },
                [ SvgAnimationEffect.CancelFrame; SvgAnimationEffect.Disposed ]

type SvgAnimationCallbacks =
    {
        ApplySample: string -> uint64 -> uint64 -> AnimationClipSample -> unit
        DispatchCues: string -> uint64 -> CueOccurrence list -> unit
        Refused: SvgAnimationRefusal -> unit
        Dispose: unit -> unit
    }

type SvgAnimationHostObservation =
    {
        AuthorityRevision: uint64
        PresentationRevision: uint64
        Status: SvgAnimationStatus
        MotionPreference: SvgMotionPreference
        ActiveEffectCount: int
        OwnedListenerCount: int
        ScheduledFrameCount: int
        IsDisposed: bool
    }

module private AnimationDom =
    [<Emit("requestAnimationFrame($0)")>]
    let requestFrame (_callback: float -> unit) : float = jsNative

    [<Emit("cancelAnimationFrame($0)")>]
    let cancelFrame (_token: float) : unit = jsNative

    [<Emit("document.hidden")>]
    let hidden () : bool = jsNative

    [<Emit("window.matchMedia('(prefers-reduced-motion: reduce)').matches")>]
    let prefersReducedMotion () : bool = jsNative

[<Sealed>]
type SvgAnimationHost(callbacks: SvgAnimationCallbacks, config: SvgAnimationPolicyConfig, authorityRevision: uint64) =
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()

    let initialMotion =
        if AnimationDom.prefersReducedMotion () then
            SvgMotionPreference.Reduced
        else
            SvgMotionPreference.Full

    let mutable state =
        SvgAnimationPolicy.initialize config authorityRevision initialMotion

    let mutable frame: float option = None
    let mutable lastTimestamp: float option = None

    let cancelFrame () =
        frame |> Option.iter AnimationDom.cancelFrame
        frame <- None
        lastTimestamp <- None

    let rec scheduleFrame () =
        let _, _, status, _, activeCount = SvgAnimationPolicy.observe state

        if status = SvgAnimationStatus.Running && activeCount > 0 && frame.IsNone then
            frame <-
                Some(
                    AnimationDom.requestFrame (fun timestamp ->
                        frame <- None

                        let delta =
                            lastTimestamp
                            |> Option.map (fun previous -> TimeSpan.FromMilliseconds(max 0.0 (timestamp - previous)))
                            |> Option.defaultValue TimeSpan.Zero

                        lastTimestamp <- Some timestamp
                        apply (SvgAnimationObservation.Frame delta)
                        scheduleFrame ())
                )

    and interpret effects =
        for effect in effects do
            match effect with
            | SvgAnimationEffect.ApplySample(id, authority, revision, sample) ->
                callbacks.ApplySample id authority revision sample
            | SvgAnimationEffect.CueBatch(id, authority, cues) -> callbacks.DispatchCues id authority cues
            | SvgAnimationEffect.EffectRefused reason -> callbacks.Refused reason
            | SvgAnimationEffect.ScheduleFrame -> scheduleFrame ()
            | SvgAnimationEffect.CancelFrame -> cancelFrame ()
            | SvgAnimationEffect.Disposed -> callbacks.Dispose()
            | SvgAnimationEffect.EffectCompleted _ -> ()

    and apply observation =
        let next, effects = SvgAnimationPolicy.update config observation state
        state <- next
        interpret effects

    let listen (target: EventTarget) (name: string) (handler: Event -> unit) =
        target.addEventListener (name, handler)
        listeners.Add(target, name, handler)

    do
        listen (document :> EventTarget) "visibilitychange" (fun _ ->
            if AnimationDom.hidden () then
                apply SvgAnimationObservation.Pause
            else
                apply SvgAnimationObservation.Resume)

        listen (window :> EventTarget) "blur" (fun _ -> apply SvgAnimationObservation.Pause)

    member _.Start request =
        apply (SvgAnimationObservation.Start request)

    member _.Pause() = apply SvgAnimationObservation.Pause
    member _.Resume() = apply SvgAnimationObservation.Resume

    member _.Seek(effectId, elapsed) =
        apply (SvgAnimationObservation.Seek(effectId, elapsed))

    member _.ReplaceAuthority revision =
        apply (SvgAnimationObservation.ReplaceAuthority revision)

    member _.SetMotionPreference preference =
        apply (SvgAnimationObservation.SetMotionPreference preference)

    member _.Observe() =
        let authority, presentation, status, motion, activeCount =
            SvgAnimationPolicy.observe state

        {
            AuthorityRevision = authority
            PresentationRevision = presentation
            Status = status
            MotionPreference = motion
            ActiveEffectCount = activeCount
            OwnedListenerCount =
                if status = SvgAnimationStatus.Disposed then
                    0
                else
                    listeners.Count
            ScheduledFrameCount = if frame.IsSome then 1 else 0
            IsDisposed = status = SvgAnimationStatus.Disposed
        }

    interface IDisposable with
        member _.Dispose() =
            let _, _, status, _, _ = SvgAnimationPolicy.observe state

            if status <> SvgAnimationStatus.Disposed then
                apply SvgAnimationObservation.Dispose

                for target, name, handler in listeners do
                    target.removeEventListener (name, handler)

                listeners.Clear()

[<RequireQualifiedAccess>]
module SvgAnimationHost =
    let defaultConfig = { MaxDecorativeEffects = 64 }
