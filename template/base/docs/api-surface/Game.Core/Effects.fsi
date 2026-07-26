// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Effects.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// Why a damage amount was dealt, which is a different axis from what it was made of. Cover, armor,
/// and the `Armored` trait reduce *declared attacks* and nothing else; a unit shoved into a wall and
/// a unit standing in lava are not being attacked, and a poison tick is not either. A pipeline with
/// no source tag cannot express that distinction, and the distinction is load-bearing wherever
/// scoring pays a bonus for kills that were not direct damage.
///
/// Orthogonal to the damage *kind* (Physical / Frost / Electric / …), which stays generic in `'K`.
///
/// **Qualified.** `Collision` is a name a physics-adjacent consumer is near-certain to define — and
/// `Physics` is one — so `open FS.GG.Game.Core` must not inject it. Write `Source.Declared`.
[<RequireQualifiedAccess>]
type Source =
    /// A deliberate attack by an actor. The only source cover and armor may reduce.
    | Declared
    /// Contact damage from being displaced into something (or something into you).
    | Collision
    /// Damage the terrain deals for standing on it — lava, spikes, a chasm floor.
    | Environmental
    /// A tick of an already-applied status effect: poison, burn, bleed.
    | Periodic

/// Public contract type exposed by the FS.GG.Game.Core package.
/// One incoming hit, before mitigation: what it is made of (`Kind`, generic — `Game.Core` never
/// learns what Frost is), why it is being dealt (`Source`), and the amount that left the shooter
/// (`Base`). The amount is already built and already rolled: stat mods, crits, and RNG belong to the
/// caller's stat block and its own `Rng` stream, not here.
type Damage<'K> =
    { Kind: 'K
      Source: Source
      Base: float }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// What a `Stage` did to the running amount. `Halt` stops the pipeline immediately and skips every
/// later stage — **including the floor**, which is the entire point: a `max` at the end of a pipeline
/// erases every zero the pipeline produced, so a full immunity that returned `StageResult.Continue 0.0`
/// would be lifted back to the floor and deal damage.
///
/// **Qualified**, as `Source` is, and more urgently: `Continue` and `Halt` are ordinary words, and the
/// caller writing a `Stage` is the one most likely to have their own.
[<RequireQualifiedAccess>]
type StageResult =
    /// The amount, passed on to the next stage.
    | Continue of float
    /// The amount, final. No later stage runs, and the trace records which stage stopped it.
    | Halt of float

/// Public contract type exposed by the FS.GG.Game.Core package.
/// One named step of a mitigation pipeline: given the target, the incoming hit, and the running
/// amount, produce the next amount. `Run` is handed no world and no other target, so a stage *cannot*
/// observe anything but its own target — which is what makes `applyAll` permutation-invariant
/// structurally rather than by test.
///
/// `Name` is what a `DamageTrace` reports, so name a stage after the rule it encodes ("armor",
/// "cover", "vulnerable"), not after its arithmetic.
type Stage<'T, 'K> =
    { Name: string
      Run: 'T -> Damage<'K> -> float -> StageResult }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The full audit of one hit against one target: the amount the pipeline started from, every stage's
/// output in order, the final amount, and — if the pipeline exited early — the name of the stage that
/// stopped it.
///
/// `Halted` is not diagnostic decoration. It is what the caller reads to suppress a rider status
/// effect: a frost bolt that a fully-immune target shrugged off must not apply its `Slow`, and the
/// only way to know that happened is that `resist` halted.
///
/// `Seed` is the amount the first stage was handed: `damage.Base` under `pipeline`, and
/// `damage.Base * multiplier` under `applyAll`. It is **not** a `Step` — the transport multiplier is a
/// seed precisely so that it cannot be reordered after `subtract` — but without it the audit omits the
/// one input every stage is measured against. A 30-damage blast that deals 9 to a rim target traces as
/// `Steps = [ "subtract", 9.0 ]`, and nothing says whether mitigation ate 21 or transport did.
/// `Seed = 15.0` settles it: transport took 15, armor took 6.
///
/// `Seed` is an **amount, not a provenance**. A ×0.5 blast of 30 and a ×1.0 blast of 15 both seed at
/// `15.0` and trace identically, because after seeding they *are* the same hit. Recovering the
/// multiplier needs the hit it came from — it is `Seed / damage.Base`, and `damage` is the caller's.
///
/// Non-finite seeds degrade to `0.0`, as every other amount here does.
type DamageTrace =
    { Seed: float
      Final: float
      Halted: string voption
      Steps: (string * float) list }

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The **mitigation** layer: what an already-built, already-transported hit becomes once the target's
/// armor, cover, resistances, and immunities have had their say — plus the status-effect list that
/// hit may have riding on it. Pure, total (a non-finite input or a hostile `Stage` can never put a
/// `NaN` in a `DamageTrace`), and deterministic: a left fold over a list, no `dt`, no clock, no sort.
///
/// **`Effects` never queries space.** A *region operator* — anything of shape
/// `world -> ('T * float) list`, of which `Ballistics.splash` is one and a Tesla chain traversal is
/// another — decides *who* is hit and what fraction of the shot reached them. This module consumes
/// that list; it does not build it. Half the game corpus uses a region and no pipeline (a blast that
/// kills whatever it contains); a quarter uses a pipeline and no region (a single-target melee swing).
/// They are separate concerns and this module owns exactly one of them.
///
/// Consequently `Effects` reaches up to nothing — not even `Primitives`. Both the target `'T` and the
/// damage kind `'K` are generic.
///
/// **Ordering.** Mitigation runs amplifiers → multiplicative reductions → subtractive reductions →
/// floor. Armor's documented meaning is "these N points never get through", and that sentence is only
/// true when the subtraction is the last thing to touch the number: put a multiplier after it and
/// armor's effective value silently varies with distance from the blast centre. The order is not baked
/// in — `pipeline` takes a `Stage` list and the caller owns it — but that is the order to write down.
///
/// **Durations are tick counts, never seconds.** `Active<'E>.TicksRemaining` is an `int` decremented
/// once per fixed step. A `float` duration accumulated per frame drifts a replay apart; an `int`
/// decrement cannot. There is no `dt` anywhere in this surface, so there is nothing to accumulate
/// incorrectly, and a tick is whatever the caller drains — a 60 Hz step, or a turn-based round.
[<RequireQualifiedAccess>]
module Effects =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Run one hit against one target through `stages`, seeded at `damage.Base`, and report every
    /// step. Stages run left to right; the first `Halt` ends the walk and names itself in
    /// `DamageTrace.Halted`. A non-finite `Base`, or a non-finite amount out of any stage, contributes
    /// `0.0` rather than poisoning the total — so no `DamageTrace` field is ever `NaN`.
    ///
    /// `DamageTrace.Seed` is `damage.Base`, sanitised.
    ///
    /// Use `applyAll` for the area case: it seeds each target's pipeline at the transport multiplier
    /// the region assigned it, which is what puts transport *before* mitigation by construction.
    val pipeline: stages: Stage<'T, 'K> list -> target: 'T -> damage: Damage<'K> -> DamageTrace

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The whole area application. `targets` is exactly what a region operator returned — each target
    /// paired with the scalar that region assigned it (`Ballistics.splash`'s distance falloff, a
    /// chain's `chainFalloff^k` in the jump index, or a uniform `1.0`).
    ///
    /// Each target's pipeline is **seeded** at `damage.Base * multiplier`. The multiplier is the
    /// initial amount, not a `Stage` in the list, and the difference matters: a stage could be
    /// reordered after `subtract`, at which point falloff would start protecting the attacker from the
    /// target's armor. A seed cannot be reordered. The seeded amount is reported as `DamageTrace.Seed`,
    /// so the audit records where mitigation started even though transport is not a `Step`; the
    /// multiplier itself is `Seed / damage.Base`.
    ///
    /// Results are returned in the order `targets` was given. A non-finite multiplier seeds `0.0`.
    /// No stage can observe another target, so the result multiset is independent of that order.
    val applyAll:
        stages: Stage<'T, 'K> list -> damage: Damage<'K> -> targets: ('T * float) list -> ('T * DamageTrace) list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Multiply by `(1 + bonusOf target)` — the `Vulnerable`/`Exposed` amplifier. Runs **first**, and
    /// therefore before armor: an anti-armor upgrade worded "incoming damage ×(1+bonus)" only
    /// overcomes armor if it amplifies the amount armor then subtracts from. Applied after the
    /// subtraction it merely scales what armor already let through, which is a different upgrade that
    /// nobody chose.
    val amplify: bonusOf: ('T -> float) -> Stage<'T, 'K>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Multiply by `(1 − resistOf target kind)`, the elemental resistance stage — **except** that a
    /// resistance of `1.0` or greater `Halt`s at `0.0` instead of multiplying.
    ///
    /// Folding immunity into this stage is the reason it exists rather than being an `amplify` the
    /// caller assembles. Written as a plain multiply, a full immunity produces `0.0`, and a later
    /// `floorAt 1.0` lifts it straight back to `1.0` — "immune" deals damage, and the rider status
    /// effect is applied too because nothing observed the immunity. Halting makes that unwritable: the
    /// floor never runs, and `DamageTrace.Halted` tells the caller to suppress the rider.
    ///
    /// Halting at `> 1.0` as well as at `1.0` is deliberate: a resistance above one would otherwise
    /// multiply by a negative number and *heal* the target.
    ///
    /// The other end is **not** guarded: a resistance below `0.0` multiplies by more than one and
    /// amplifies the hit. That is a coherent reading of "negative resistance is vulnerability", so it is
    /// permitted — but it means `resistOf` is trusted to stay in `[0, 1]`, and a sign error in a stat
    /// fold reads as a damage bonus rather than as an error. Amplification has its own stage.
    val resist: resistOf: ('T -> 'K -> float) -> Stage<'T, 'K>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Subtract `amountOf target kind` — armor, cover, damage reduction. Runs **after** every
    /// multiplicative stage and **before** the floor, and may legitimately drive the running amount
    /// negative; `floorAt` is what decides where the bottom is.
    val subtract: amountOf: ('T -> 'K -> float) -> Stage<'T, 'K>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Clamp the running amount up to `minimum` (`max minimum amount`). Runs **last**, and never runs
    /// at all after a `Halt`.
    ///
    /// The floor is a game-defining parameter, never a module constant. A tower-defense floors at `1`
    /// so that a maxed-armor creep still chips; a tactics game floors at `0` and *depends* on it, so
    /// that a unit on a cover tile survives an attack weaker than its cover. One constant, and a
    /// documented acceptance criterion inverts. A non-finite `minimum` degrades to a floor of `0.0`.
    val floorAt: minimum: float -> Stage<'T, 'K>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// `Halt` at `0.0` when `predicate target damage` holds — a categorical immunity that is not a
    /// resistance number: a ghost that physical weapons cannot touch, a structure that no melee
    /// reaches. Reads the whole `Damage`, so it can gate on `Kind` and `Source` together.
    val immuneWhen: predicate: ('T -> Damage<'K> -> bool) -> Stage<'T, 'K>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Run `inner` only when the hit's `Source` is one of `sources`; otherwise pass the running amount
    /// through untouched. The combinator that makes cover, armor, and damage-reduction traits mean
    /// "against declared attacks" — `gatedBy [Source.Declared] (subtract cover)` — and that lets a
    /// poison tick bypass armor by being `Source.Periodic`.
    ///
    /// The gated stage keeps `inner`'s name in the trace, whether or not it fired, so a trace of a
    /// skipped stage still reads as the rule it encodes.
    val gatedBy: sources: Source list -> inner: Stage<'T, 'K> -> Stage<'T, 'K>

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// One live instance of a status effect on one target. `TicksRemaining` counts **fixed steps**,
    /// never seconds; the conversion from authored seconds happens once, where the content is
    /// authored, and is never re-derived per frame.
    type Active<'E> =
        { Effect: 'E
          TicksRemaining: int }

    /// Public contract type exposed by the FS.GG.Game.Core package.
    /// What a second application of an effect that is already active does. Four cases cover the corpus;
    /// naming them is the whole contribution.
    ///
    /// The policy is a property of the *effect*, not of the game — Slow and Poison want different ones
    /// in the same tower-defense — and most specs never state it. An unstated policy is a coin flip
    /// that only shows itself when two sources of the same effect overlap.
    type Policy<'E> =
        /// One instance. Reapplying keeps the incoming effect and sets the remaining ticks to
        /// `max(old, new)`, so a reapplication can extend but never cut short. Stun.
        | Refresh
        /// One instance, the strongest. An application whose `magnitude` is greater than **or equal
        /// to** the active one's replaces it and sets the remaining ticks to `max(old, new)`; a weaker
        /// one changes nothing. The `>=` is load-bearing — it is what lets a second tower of the same
        /// type re-up a slow it cannot out-strengthen. `magnitude` is oriented so that larger is
        /// stronger, so a slow whose factor is lower when stronger passes `fun e -> 1.0 - e.Factor`.
        ///
        /// The `max` is `Refresh`'s, for `Refresh`'s reason: replacement *refreshes*, and a refresh may
        /// extend but never cut short. Taking the incoming duration wholesale would let a short-variant
        /// application of equal magnitude erase most of a long one already running — invisible in a
        /// tower-defense where every slow of a tower type shares a base duration, and a live bug the
        /// moment two sources of one effect differ in duration.
        | Strongest of magnitude: ('E -> float)
        /// Independent, additive instances, appended in application order while fewer than `cap` are
        /// active. At the cap the application is a **no-op** — it does not refresh, replace, or evict
        /// the oldest stack. Poison.
        | Stacks of cap: int
        /// The first application wins; every later one, of any magnitude or duration, is a no-op.
        | Once

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Apply `incoming` to `actives` under `policy` and return the new list. `actives` are the live
    /// instances of the **same effect kind** as `incoming` — the caller keys them by kind, because
    /// `'E` is generic and `Game.Core` cannot compare two effects for identity.
    ///
    /// Order is preserved: `Stacks` appends, and every other policy yields at most one instance. So the
    /// result is a deterministic function of the application order the caller chose.
    ///
    /// `Refresh`, `Strongest`, and `Once` reconcile against the **head** of `actives` and return at most
    /// one instance, so handing them a list that already holds several — by switching an effect's policy
    /// away from `Stacks` while instances are live — discards all but the first. Re-key or drain the list
    /// when a policy changes.
    val applyEffect: policy: Policy<'E> -> incoming: Active<'E> -> actives: Active<'E> list -> Active<'E> list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Advance every active effect by one tick: decrement `TicksRemaining` and drop the instances that
    /// reach zero or below. Order-preserving, and unit-free — a tick is whatever the caller drains, so
    /// the caller names the moment (start of phase, end of round, one fixed step) rather than having
    /// this module hide it.
    ///
    /// `Effects` never sees `dt`, so a damage-over-time effect stores its **per-tick** amount and this
    /// applies a constant. There is no `dps * dt` to accumulate and no epsilon to drift.
    val tickEffects: actives: Active<'E> list -> Active<'E> list
