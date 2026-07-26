// See skill: fs-gg-testing
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Harness/Journey.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Harness

open FS.GG.Game.Core

/// Timestamp-free host events understood by a production journey. Products keep their own key,
/// pointer, menu-action, and deterministic effect-result types.
[<RequireQualifiedAccess>]
type JourneyEvent<'key, 'pointer, 'menu, 'effectResult> =
    | Start
    | MenuAction of 'menu
    | KeyInput of key: 'key * pressed: bool
    | PointerInput of 'pointer
    | Interact
    | Pause
    | Resume
    | FixedTick
    | EffectResult of 'effectResult

/// The production raw-event mapper explicitly says whether a displayed action is wired.
[<RequireQualifiedAccess>]
type JourneyDispatch<'message> =
    | Mapped of 'message list
    | Unbound of action: string

/// A product-owned adapter over its real composition root. Unlike `Playable`, it owns boot,
/// timestamp-free host mapping, message dispatch, fixed ticks, and deterministic effect results.
type ProductionJourney<'model, 'key, 'pointer, 'menu, 'effectResult, 'message, 'fingerprint> =
    { RouteId: string
      ScenarioId: string
      TestId: string
      MaxSteps: int
      Boot: unit -> 'model
      MapEvent:
        JourneyEvent<'key, 'pointer, 'menu, 'effectResult> ->
        'model ->
            JourneyDispatch<'message>
      Update: 'message -> 'model -> 'model
      FixedTick: 'model -> 'model
      ApplyEffectResult: 'effectResult -> 'model -> 'model
      IsTerminal: 'model -> bool
      Fingerprint: 'model -> 'fingerprint
      EncodeEvent: JourneyEvent<'key, 'pointer, 'menu, 'effectResult> -> string
      EncodeFingerprint: 'fingerprint -> string }

/// Runner outcome. Exhaustion and unbound displayed actions are explicit failures.
[<RequireQualifiedAccess>]
type JourneyResult =
    | Passed
    | Failed of reason: string

/// The runner route which produced the captured input. A seeded policy is identified separately
/// from replaying its captured fixed script.
[<RequireQualifiedAccess>]
type JourneyInputKind =
    | FixedScript
    | SeededPolicy

/// Opaque machine-issued receipt. Only the production-journey runner can construct one.
[<Sealed>]
type JourneyReceipt

[<RequireQualifiedAccess>]
module JourneyReceipt =
    val schemaVersion: JourneyReceipt -> int
    val runnerIdentity: JourneyReceipt -> string
    val runnerVersion: JourneyReceipt -> string
    val compositionAuthority: JourneyReceipt -> string
    val origin: JourneyReceipt -> Origin
    val routeId: JourneyReceipt -> string
    val scenarioId: JourneyReceipt -> string
    val testId: JourneyReceipt -> string
    val inputKind: JourneyReceipt -> JourneyInputKind
    val inputIdentity: JourneyReceipt -> string
    val inputDigest: JourneyReceipt -> string
    val scriptDigest: JourneyReceipt -> string
    val traceDigest: JourneyReceipt -> string
    val initialFingerprintDigest: JourneyReceipt -> string
    val terminalFingerprintDigest: JourneyReceipt -> string
    val terminalPredicateIdentity: JourneyReceipt -> string
    val terminalPredicateReached: JourneyReceipt -> bool
    val result: JourneyReceipt -> JourneyResult
    val steps: JourneyReceipt -> int
    val maxSteps: JourneyReceipt -> int

/// A journey trace, captured event stream, final model, and runner-issued receipt.
type JourneyRun<'model, 'event, 'fingerprint> =
    { Trace: Trace<'fingerprint>
      Captured: 'event list
      Final: 'model
      Receipt: JourneyReceipt }

[<RequireQualifiedAccess>]
module Journey =
    val runScriptWithIdentity:
        inputIdentity: string ->
        terminalPredicateIdentity: string ->
        adapter: ProductionJourney<'model, 'key, 'pointer, 'menu, 'effectResult, 'message, 'fingerprint> ->
        script: JourneyEvent<'key, 'pointer, 'menu, 'effectResult> list ->
            JourneyRun<'model, JourneyEvent<'key, 'pointer, 'menu, 'effectResult>, 'fingerprint>
