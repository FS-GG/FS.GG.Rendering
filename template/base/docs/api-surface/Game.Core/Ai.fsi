// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.12.0 (src/Game.Core/Ai.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A stable, comparable identity for one decision-making agent. It is the **iteration order** of the
/// decision layer: agents are stepped in ascending `AgentId`, never in `Map`/`HashSet` order, because
/// hash order is a function of insertion history and would make a replay diverge from the recording.
[<Struct>]
type AgentId = AgentId of int

/// Public contract type exposed by the FS.GG.Game.Core package.
/// One perceived entity, as the perception layer saw it. `LastSeenTick` is what makes a sighting decay
/// into a ghost: a sighting whose `LastSeenTick` equals the view's tick is currently *spotted*, and one
/// that is older is a *ghost* — a decaying memory of where something was, which is exactly what an AI
/// should shoot at when it has lost contact.
///
/// `Seen` is the caller's own payload (hit points, gun, whatever policy needs). The vocabulary carries
/// it without inspecting it, so this type never grows a game's stat block.
[<Struct>]
type Sighting<'T> =
    { /// The perceived entity's identity.
      Agent: AgentId
      /// Where it was when last seen — a ghost's position is stale by construction.
      Position: Point
      /// The caller's payload. Opaque to this module.
      Seen: 'T
      /// The tick at which this sighting was taken.
      LastSeenTick: int }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// **The fog boundary, made a compile-time guarantee.** A `TeamView` is what one team knows: entities it
/// currently sees, plus decaying ghosts of entities it has lost. It is deliberately **abstract** — it has
/// no public constructor, no public fields, and no member that returns anything but sightings. There is
/// no expression of type `TeamView<'T>` from which the world model can be recovered.
///
/// That is the whole point. Hand the decision layer a `TeamView` and it *cannot* consult the model, so
/// it can be flanked (it genuinely does not know you are there), it shoots at ghosts, and blinding it is
/// a real play. The moment someone "fixes" a stuck agent by passing it the full model, the signature
/// stops compiling rather than the behaviour silently becoming omniscient.
///
/// The intended shape of a decision function is therefore:
///
/// ```fsharp
/// val decide: TeamView<'T> -> Agent -> Rng -> Command * Rng
/// ```
///
/// Nothing in this module can widen that. Build one with `Ai.view`.
///
/// **The one hole, named.** The guarantee is "no *accessor* yields the world", not "the world cannot be
/// smuggled". A caller who instantiates `'T` as its own `Model` puts the world inside every `Sighting`,
/// and the compiler cannot object — `'T` is theirs. What the type does buy is that the leak becomes a
/// single, explicit, greppable type argument at the construction site (`Ai.view : ... -> Sighting<Model>
/// list -> _`) instead of an omniscient `decide` that reads correctly. Choose `'T` to be the facts
/// perception yielded — hit points, gun, hull bearing — not the model that yielded them.
[<Sealed>]
type TeamView<'T>

/// Public contract type exposed by the FS.GG.Game.Core package.
/// **Difficulty is a knob vector, never a stat multiplier.** Every field here takes *time* or *precision*
/// away from the agent; none of them touches a game stat. A hard agent reacts sooner, aims tighter, spots
/// more often and values its own safety correctly — it does not get more damage or more penetration.
///
/// This is not stylistic. An agent that cheats on stats cannot teach the player the game's rules, because
/// the rules it plays by are not the rules on screen. An agent that is merely faster and steadier is
/// beatable by understanding the same systems the player already has.
[<Struct>]
type Difficulty =
    { /// Ticks between spotting a target and the first response. Higher is easier.
      ReactionTicks: int
      /// Standard deviation of the Gaussian aim error, in radians. Higher is easier.
      AimErrorSigma: float
      /// Ticks between perception refreshes. Higher is easier (staler ghosts).
      SpotCycleTicks: int
      /// Whether the agent aims at weak points rather than centre-mass.
      UsesWeakPointTargeting: bool
      /// How much the agent weighs danger against progress when costing a route. Higher is more cautious.
      ThreatWeight: float }

/// Public contract module exposed by the FS.GG.Game.Core package.
/// The thin, pure vocabulary the decision layer needs, and nothing more. This is **not** a behaviour-tree
/// framework, an ECS, or utility-AI-as-a-library: it is a handful of total functions over a caller-supplied
/// oracle. Perception, posture, positioning and aiming are *policy* and stay with the game.
///
/// Everything here is pure, total and deterministic: no wall clock, no ambient RNG, no `Map`/`HashSet`
/// iteration order escaping into a result. Identical inputs yield byte-identical outputs across runs and
/// platforms, so it is safe to call from a replayed simulation step.
[<RequireQualifiedAccess>]
module Ai =

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Build a `TeamView` from raw sightings, at `tick`. A sighting older than `ghostLifetimeTicks` is
    /// **forgotten** (dropped); one taken at `tick` is *spotted*; anything in between is a *ghost*.
    ///
    /// Duplicate `AgentId`s collapse to the freshest sighting, so a caller may concatenate several
    /// spotters' reports without deduplicating first. Ties on `LastSeenTick` keep the earliest in input
    /// order, which is total and therefore replay-stable.
    ///
    /// Total: `ghostLifetimeTicks < 0` is read as `0` (remember nothing but what is currently spotted); a
    /// sighting from the future (`LastSeenTick > tick`) is clamped to *spotted* rather than rejected.
    val view: tick: int -> ghostLifetimeTicks: int -> sightings: Sighting<'T> list -> TeamView<'T>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The tick this view was taken at.
    val viewTick: view: TeamView<'T> -> int

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Entities currently visible, ascending by `AgentId`.
    val spotted: view: TeamView<'T> -> Sighting<'T> list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Decaying memories — seen once, not seen now, not yet forgotten — ascending by `AgentId`. Shooting
    /// at a ghost is correct behaviour, not a bug.
    val ghosts: view: TeamView<'T> -> Sighting<'T> list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Everything the team knows: `spotted` then `ghosts` merged, ascending by `AgentId`.
    val known: view: TeamView<'T> -> Sighting<'T> list

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// What the team knows about one entity, or `ValueNone` if it is unseen or forgotten.
    val tryFind: agent: AgentId -> view: TeamView<'T> -> Sighting<'T> voption

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The agent's own random sub-stream, derived from the root generator and the agent's **identity**.
    ///
    /// An agent that draws from the root RNG makes every other agent's draws depend on how many agents are
    /// alive and in what order they were stepped — a heisenbug that only surfaces in a replay, when a unit
    /// dies one tick earlier than it did in the recording and every subsequent aim roll shifts.
    ///
    /// Keyed on `AgentId`, **not** on position in a list: sequentially `Rng.split`-ing down a sorted agent
    /// list has the same bug one level down, because the death of a low-id agent shifts every later agent's
    /// stream. Deriving from the id itself is stable under spawn and death alike.
    ///
    /// Deterministic and pure. Distinct ids yield decorrelated streams; the same `(root, id)` always yields
    /// the same stream.
    val substream: root: Rng -> agent: AgentId -> Rng

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Whether a layer with the given cadence runs on this tick — `true` exactly when `tick` is a multiple
    /// of `cadenceTicks`. Layer by cost, cheapest first: aiming every tick, positioning every 30, the
    /// threat map every 15. Recomputing a threat map per agent per tick is the default mistake.
    ///
    /// Total: `cadenceTicks <= 0` never runs (`false`), rather than dividing by zero. Negative ticks are
    /// handled by a floored modulus, so a countdown before the match starts does not alias.
    val due: cadenceTicks: int -> tick: int -> bool

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A Gaussian aim error in radians, drawn from the agent's own sub-stream: `sigma` is the difficulty
    /// knob, not a stat. Returns the deviation and the advanced generator (pure — `rng` is unchanged).
    ///
    /// Total: a non-finite or non-positive `sigma` yields exactly `0.0` and **does not advance** the
    /// generator, so a perfectly-accurate agent draws no randomness and cannot desynchronise a replay from
    /// one that has aim error enabled for a different agent.
    val aimError: sigma: float -> rng: Rng -> struct (float * Rng)

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// The threat map: `danger(cell) = Σ dps × hasLos(cell, source) × inRange(cell, source)`, summed over
    /// every threat source. This is where the **weighting and summing** live — deliberately here, in the
    /// decision layer, and not in `Pathfinding`, which owns navigation and not policy.
    ///
    /// Each source is `(position, dps, rangeInCells)`; a source is in range of a cell when the squared
    /// Euclidean cell distance is at most `rangeInCells²` (accumulated in `int64`, so a large range cannot
    /// overflow). `hasLos` is a caller-supplied oracle — this module never casts a ray itself.
    ///
    /// **Order-independent by construction.** Float addition is not associative, so the sum is taken over
    /// sources in a canonical order — ascending `(Col, Row, dps, range)` — rather than in the order the
    /// caller happened to pass them. Without that, the map would depend on `SpatialGrid` insertion order
    /// and a replay would diverge the moment an entity spawned. Permuting `sources` yields a byte-identical
    /// map.
    ///
    /// Total: a source whose `dps` is not finite contributes `0.0`; a negative `rangeInCells` is read as
    /// out of range everywhere. Cells absent from `cells` are absent from the result — this never
    /// enumerates a grid it was not given.
    val threatField:
        hasLos: (Cell -> Cell -> bool) ->
        sources: (Cell * float * int) list ->
        cells: Cell list ->
            Map<Cell, float>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// A **flee field**, built the way `Pathfinding.distanceField` says one is built: scale a desire map by
    /// a negative `coefficient` (≈ `-1.2`) and re-relax it, then roll downhill with `Pathfinding.flowField`.
    /// The coefficient is a tuning constant — game policy, not navigation — which is precisely why it lives
    /// here rather than in `Pathfinding`.
    ///
    /// Relaxation sweeps cells in ascending `(Col, Row)` and lowers any cell that sits more than one step
    /// above its cheapest in-field neighbour, repeating until a sweep changes nothing or `maxPasses` sweeps
    /// have run. Bounding the passes keeps it total on a pathological field; the result is deterministic
    /// either way, because the sweep order is fixed.
    ///
    /// Membership in `desire` is the walkability predicate, exactly as in `Pathfinding.flowField`, and the
    /// same no-corner-cutting rule applies under `EightWay`. Pass the same `neighbourhood` you intend to
    /// roll downhill with.
    ///
    /// Total: `maxPasses <= 0` returns the scaled field unrelaxed. A cell is dropped rather than poisoning
    /// its neighbours whenever its value is non-finite **or becomes non-finite once scaled** — so a
    /// non-finite `coefficient`, or a desire near `Double.MaxValue` that overflows under a routine `-1.2`,
    /// yields a smaller field rather than a `NaN`-poisoned one. No non-finite value is ever returned.
    val fleeField:
        neighbourhood: Neighbourhood ->
        coefficient: float ->
        maxPasses: int ->
        desire: Map<Cell, float> ->
            Map<Cell, float>

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Pick the best plan under a **total** order: highest `score`, then lowest first tie-breaker, then
    /// lowest second tie-breaker, then earliest in `plans`.
    ///
    /// The tail matters. Float scores tie — two positions that are equally good really do score equally —
    /// and a comparison that stops at the score leaves the winner at the mercy of list order, which is a
    /// function of whatever enumerated the candidates. The index tail is what makes a plan byte-reproducible.
    /// This reproduces the `turn-based-tactics` §4.9 rule exactly: *(highest score, lowest targetTile index,
    /// lowest position index)*.
    ///
    /// Total: a `NaN` score is ordered below every real score (an unscoreable plan is never preferred), so
    /// `best` returns `ValueNone` only for an empty list — never `NaN` by accident.
    val best: score: ('p -> float) -> tie: ('p -> struct (int * int)) -> plans: 'p list -> 'p voption

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// An **integer influence map** from strengthed sources (roadmap 3.2) — a thin wrapper over
    /// `Pathfinding.distanceField`. Each `(source, strength)` contributes `max(0, strength - distance)`
    /// at every cell, where `distance` is the `distanceField` cost from that source (so influence is
    /// `strength` at the source and falls off by 1 per `baseStep` of distance, in the same units).
    /// Contributions combine by **max** — the strongest nearby source dominates — and cells whose
    /// influence would be `<= 0` are **absent**. Empty `sources` yields an empty map.
    ///
    /// Inherits `distanceField`'s `cost` convention (`cost c <= 0` impassable), its `maxVisited` bound,
    /// and its determinism, so `influenceMap` is a pure, byte-deterministic value.
    ///
    /// **Tension recipe.** A friendly-vs-enemy control map is the per-cell difference of two influence
    /// maps: `tension c = friendly[c] - enemy[c]` (an absent side reads as `0`). Positive tension marks
    /// friendly-controlled cells, negative enemy-controlled — the caller composes it from two
    /// `influenceMap` calls.
    val influenceMap:
        neighbourhood: Neighbourhood ->
        maxVisited: int ->
        cost: (Cell -> int) ->
        sources: (Cell * int) list ->
            Map<Cell, int>

/// Public contract module exposed by the FS.GG.Game.Core package.
/// A difficulty ladder expressed purely as the knob vector. Compare the three: every difference is time or
/// precision. No stat is touched, and there is no field here with which one could be.

[<RequireQualifiedAccess>]
module Difficulty =

    /// Public contract value exposed by the FS.GG.Game.Core package.
    /// Slow to react, wide-shooting, centre-mass aiming, rarely looks. Reckless — it barely prices danger.
    val easy: Difficulty

    /// Public contract value exposed by the FS.GG.Game.Core package.
    /// The reference rung.
    val normal: Difficulty

    /// Public contract value exposed by the FS.GG.Game.Core package.
    /// Reacts in a few ticks, aims tight, spots often, targets weak points, and respects a threat map.
    val hard: Difficulty

    /// Public contract function exposed by the FS.GG.Game.Core package.
    /// Sanitise a hand-authored or interpolated knob vector into one the layers can consume: tick counts are
    /// floored at `0`, and `AimErrorSigma`/`ThreatWeight` are floored at `0.0` with a non-finite value read
    /// as `0.0`. Idempotent, and the identity on every rung of the ladder above.
    val clamp: difficulty: Difficulty -> Difficulty
