---
name: fs-gg-persistence
description: Give a generated FS.GG.UI product save/load — persistence as pure values (save/load/delete a versioned slot) recorded at the host boundary, no file I/O in update.
---

# Persistence (Save/Load) Capability

## Scope

Use this skill to give a game/sim product **save slots**: writing a save, loading a slot, and
deleting a slot. Persistence here is **requested as pure values** — your `update` returns
`PersistenceEffect` values, it never touches the filesystem. You serialize your own `Model` into an
opaque payload and stamp a save-format version; a record-only interpreter folds the requests into
ordered evidence, so the whole thing is deterministic and testable with no writable save location.
Real file I/O (and dispatching a loaded save back to your model) is the host's job (a deferred
backend); this skill covers requesting save/load and proving what was requested. This skill
materializes for the `game` and `sample-pack` profiles.

> **Building the file backend rather than consuming it?** Then the testing guidance below is written
> for the wrong reader, and following it will hand you a suite that passes against a backend that
> writes nothing. Read [If you own the backend](#if-you-own-the-backend) first.

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Canvas/Persistence.fsi` — the `PersistenceEffect` request DU
  (`Save`/`Load`/`DeleteSlot`), the `SaveSlot`/`SavePayload` identifiers, the `SaveEnvelope` record,
  the `PersistenceEvidence` record, and the `Persistence` module (smart constructors + the
  record-only `interpret`/`record`). Shipped in `FS.GG.UI.Canvas`, referenced on the
  `game` and `sample-pack` profiles. That package carries **only** the pure elements, the render loop,
  and this persistence request surface — the pinned Canvas api-surface is exactly `Elements.fsi` and
  `Persistence.fsi`. It carries **no audio** (that vocabulary was retired at the Canvas `0.3.0` major;
  audio is `FS.GG.Audio.Core`/`FS.GG.Audio.Host`, a separate package your product pins itself) and
  **no simulation primitives** (those are `FS.GG.Game.Core`). [[fs-gg-audio]] says the same thing from
  its side; if the two ever disagree, the api-surface bundled with your product settles it.

All helpers are **total**: the save-format version is clamped to `>= minVersion` at the boundary, and
no helper throws or performs I/O. The payload is **opaque** — the framework never parses it.

> **`Persistence.interpret` PERSISTS NOTHING.** It records your requests into
> `PersistenceEvidence.Requested` and drops them. No file is written, read or deleted — not here, and
> not later by a host: no `ViewerEffect` case carries a `PersistenceEffect`, so no host runner will
> ever see one. A product that calls it has saved nothing.
>
> The name is a trap, and a known one — but not in the way you would guess. It is **not** that
> `interpret*` means *perform the effect* everywhere else in this framework and this one is the lone
> exception. **Nothing named `interpret` here performs anything.** The surface your product pins
> carries exactly two, and both are pure folds that hand you back a value: this one, and
> `Audio.interpret` — which calls itself a *"record-only interpreter"* with *"no device access"* and
> returns an `AudioEvidence`, precisely as this returns a `PersistenceEvidence`. Performing an effect
> is always a **host** call: `Audio.play` takes a backend, `Viewer.runApp` opens a window. **Not one
> of them is spelled `interpret`.**
>
> So do not go looking for a counter-example among its siblings — it has one, and it has this same
> shape. What `interpret` really promises you is a **downstream**: something, somewhere, that
> eventually carries your requests out. For persistence there is none, and that is the whole defect.
> The convention is what misleads you here, not an exception to it — which makes the honest reading
> the stronger one.
>
> A later framework release renames it to `interpretRecordOnly`, which says what it does — but **that
> spelling is not in the `FS.GG.UI.Canvas` your product pins**, so `interpret` is the one to call
> today.

## Requesting save/load from `update`

`PersistenceEffect` is a plain value you return from your pure `update` alongside your model change —
the same discipline as scene, input, and audio effects. You serialize your `Model` yourself (your
choice of format) and hand the bytes over as the payload:

```fsharp
open FS.GG.UI.Canvas

type Msg = CheckpointReached | ContinueGame | EraseSave

// You own serialization — the framework carries the bytes verbatim, never parsing them.
let serialize (model: Model) : string = // JSON, a custom encoding, whatever you like
    ...

// update stays pure: it maps a Msg to a requested PersistenceEffect. No filesystem, no IO.
let persistFor (model: Model) (msg: Msg) : PersistenceEffect =
    match msg with
    | CheckpointReached -> Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") (serialize model))
    | ContinueGame      -> Persistence.load (SaveSlot "slot-1")     // result comes back as a Msg (deferred backend)
    | EraseSave         -> Persistence.deleteSlot (SaveSlot "slot-1")
```

`SaveSlot` is an opaque name **you** own — the framework does not map it to a path. The **version**
you stamp lets a future load migrate or reject an old save; that migration policy is yours, in your
own deserialize step.

## Deciding *when* to save: the cue seam, and the `Init` blind spot

Products decide their save/load requests in **one place**, by analogy with `AudioCues.forTransition`
([[fs-gg-audio]]): a `SaveCues.forTransition : Msg -> Model -> Model -> PersistenceEffect list` that
maps a **transition** to the effects it implies. Writing one is the normal thing to do; this section is
about the hole in the pattern.

`forTransition` is a function of a **transition**, and **the initial model does not make one.** It
comes out of `initialModel`, and nothing is ever dispatched into it — so anything the initial state
*implies* is never requested.

**Persistence is where that bites hardest, and it is the reason to read this twice.** Save/load state
is *by definition* state you **load** rather than transition into, so the blind spot is not an edge
case here — it is the main path. Restore a save in `initialModel` and the model is *correct*: the game
genuinely is at the restored checkpoint. And **nothing was ever asked of the sink**, because no
transition carried a request there. Nothing catches it: no type is wrong, and **a test that asserts on
the model passes** — from inside the model, a checkpoint nobody recorded is indistinguishable from one
that was.

`Started` is the door that closes it — **and unlike audio's, you have to hang it yourself.** Audio's
seam is scaffold-wired: the generated host calls `AudioCues.forTransition Started m m` from `Init`.
**Persistence has no such host.** No `ViewerEffect` case carries a `PersistenceEffect`, so nothing in
the framework will ever call a `SaveCues` of yours — see [Package Boundary](#package-boundary), which
spells out that today there is no host at all. The seam, the `Started` case, and the call from your own
`Init` are **all product-owned**:

- give your `Msg` a `Started` case;
- from wherever you build the initial model, call `SaveCues.forTransition Started m m` yourself — the
  **same** function `Update` calls, with the initial model on both sides, so there is no separate
  startup path to drift out of sync;
- fold what it returns into the same evidence your `update`'s requests feed.

And the move that actually removes the blind spot: **stop reaching into a save inside `initialModel`,
and request the restore through the seam instead.** Then the request exists — which is the only reason
anything downstream, including your tests, can ever see it:

```fsharp
open FS.GG.UI.Canvas

type Model = { Slot: string; Score: int }
type Msg = Started | CheckpointReached | ContinueGame | EraseSave

let serialize (model: Model) : string = string model.Score

// Nothing in the framework calls this. YOU call it from `Update`, and from `Init` as
// `forTransition Started m m` — one seam, so a save cannot be requested down a path nobody watches.
let forTransition (msg: Msg) (previous: Model) (next: Model) : PersistenceEffect list =
    match msg with
    // The initial model is LOADED, not transitioned into. Asking for the restore HERE — rather than
    // reading a save inside `initialModel` — is what makes it a request at all, and so provable.
    | Started
    | ContinueGame -> [ Persistence.load (SaveSlot next.Slot) ]
    | CheckpointReached ->
        [ Persistence.save (Persistence.saveEnvelope 1 (SaveSlot next.Slot) (serialize next)) ]
    | EraseSave -> [ Persistence.deleteSlot (SaveSlot next.Slot) ]
```

(The `Load` result cannot come *back* while the backend is deferred — that is the next pitfall down, and
it is a different defect from this one. A product that genuinely needs restored state at frame 0 still
builds it in `initialModel`; `Started` is then where it *declares* what it did, so the sink hears about
it and a test can prove it.)

**Assert at the sink, not at the model.** The only test shape that catches this class asks what the
sink was *told*, not what the model *holds* — [[fs-gg-rendering:fs-gg-testing]] works the case end to
end. While the backend is deferred the "sink" is the record-only interpreter, so that means asserting
on `PersistenceEvidence.Requested`; if you own a real backend, assert on the **files** it wrote (see
[If you own the backend](#if-you-own-the-backend)).

<!-- skill-refs: closed-ok FS-GG/FS.GG.Rendering#494 — cited as the ORIGIN of the blind spot, not as
     open work. It is the issue this very section answers, so its closure is what success looked like;
     the citation is history and stays correct closed. -->
> **This spreads by imitation, which is why the warning is here rather than only in `fs-gg-audio`.** A
> product wrote its own `SaveCues.forTransition` by analogy with the audio seam, unaided — and
> inherited the identical blind spot (FS-GG/FS.GG.Rendering#494). The pattern travels faster than the
> caveat does.

## Recording what was requested (headless-safe evidence)

`Persistence.interpret` folds a batch of requests into `PersistenceEvidence` — the requested effects
in dispatch order, with `Save` versions normalized and payloads carried verbatim. This is the
record-only interpreter: it never blocks, never touches the filesystem, and is the evidence you
assert on in tests. `Persistence.record` appends a single effect if you accumulate frame by frame.

```fsharp
open FS.GG.UI.Canvas

let evidence =
    Persistence.interpret
        [ Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") "{score:42}")
          Persistence.load (SaveSlot "slot-1")
          Persistence.deleteSlot (SaveSlot "old") ]

// evidence.Requested =
//   [ Save { Version = 1; Slot = SaveSlot "slot-1"; Payload = SavePayload "{score:42}" }
//     Load (SaveSlot "slot-1"); DeleteSlot (SaveSlot "old") ]
```

## Common pitfalls

- **Reading/writing a file inside `update`.** `update` must stay pure — return a `PersistenceEffect`
  value and let the host (or, today, the record-only interpreter) act on it. I/O in `update` breaks
  determinism and testability.
- **Expecting `Load` to return the save synchronously.** The pure surface only *requests* a load;
  the loaded payload comes back as a `Msg` your `update` handles, once a real backend lands. Today the
  interpreter just records the request.
- **Letting the framework own your format.** `SavePayload` is opaque — serialize and version your
  `Model` yourself. The framework carries the bytes verbatim and never parses them; a schema change is
  your deserialize step's job, which is why you stamp the version.
- **Expecting `Load`/`DeleteSlot` of an empty slot to error.** The interpreter records exactly what
  you request; "no such save" is a deferred-backend concern, not an exception here.
- **Expecting a save restored in `initialModel` to have *asked* for anything.** The initial model
  makes no *transition*, so `forTransition` never runs for it and every request the loaded state
  implies is silently never made — while the model is perfectly correct and a test that asserts on it
  **passes**. This is the main path for persistence, not an edge case. Handle it under `Started`; see
  [Deciding *when* to save](#deciding-when-to-save-the-cue-seam-and-the-init-blind-spot).
- **Expecting real save files in CI — *while the backend is deferred*.** With the record-only
  interpreter there is no file backend, so the evidence is the *requested* values: assert on
  `PersistenceEvidence.Requested`, not on files on disk. **If you are building the backend, invert
  this** — a `Requested`-only suite passes against a backend that writes nothing. See
  [If you own the backend](#if-you-own-the-backend).

## If you own the backend

Everything above is written for a product that *consumes* a deferred backend. **If your work item is
to build the file backend itself, the guidance above describes the trap, not the target.**

`Persistence.interpret`/`record` are **record-only**: they fold requests into
`PersistenceEvidence.Requested` and touch no filesystem. So a suite that asserts on `Requested`
**passes perfectly against a backend that writes nothing.** It exercises the framework's interpreter,
never your code — and it will stay green through every bug you ship. Requests are not effects.

Assert on the **effect**, not the request:

- **`Save` writes.** After your backend handles a `Save`, the slot exists on disk and holds the
  `SavePayload` bytes verbatim (the framework never parses or re-encodes them) under the stamped
  `Version`.
- **`Load` round-trips across a process boundary.** `Save` then `Load` of the same `SaveSlot` yields
  the same `Version` and the same `Payload` — read back from a *fresh* process, not from an
  in-memory cache that would also satisfy a same-process test.
- **`DeleteSlot` removes.** After a `DeleteSlot`, a later `Load` of that slot must not observe the
  deleted save.
- **A missing slot and a damaged slot are different outcomes.** `Load` of a never-written slot is not
  the same as `Load` of a slot whose bytes are truncated or unparseable. A backend that collapses
  both into one "no save" answer silently eats corruption. Decide what each reports, then test it.

⚠️ **The pinned surface has no load-result type — and the one you need is not yours to invent.**
`PersistenceEffect` is request-only (`Save`/`Load`/`DeleteSlot`), and `Persistence.fsi` says so
outright: *"how a real backend reports 'no such save' is a deferred concern."* On the
`FS.GG.UI.Canvas` this product pins (**0.5.0**) there is no `LoadResult`, no absent/corrupt
vocabulary, and no path that dispatches a loaded save back to `update` as a `Msg` — so a product
that needs one today cannot get it from the framework, and a private one no host dispatches buys
nothing.

Reporting a load's outcome needs a **new type on the `FS.GG.UI.Canvas` surface**, which is an
`fs-gg-rendering` change, not a product-local one. That type is **no longer an open question**: it is
designed and merged upstream, and is waiting on a release to carry it, tracked at
[FS-GG/FS.GG.Rendering#587](https://github.com/FS-GG/FS.GG.Rendering/issues/587). Follow that
release — and expect this section to change when this product's pin moves to it — rather than
inventing a private result type you would only have to unpick when the real vocabulary lands.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned save/load examples. While the backend is
deferred, assert the `PersistenceEvidence.Requested` sequence your `update` produces for a set of
events. **If you own a file backend, assert on the files it wrote instead** — see
[If you own the backend](#if-you-own-the-backend).

## Evidence

Record persistence evidence (the requested-effect sequences for representative events — a checkpoint
save, a continue-game load, an erase-save delete) under this product's `readiness/` paths. Do not copy
framework readiness reports into the product.

## Package Boundary

`PersistenceEffect`/`Persistence` are in `FS.GG.UI.Canvas` (referenced only on the
`game`/`sample-pack` profiles). Canvas depends only on Scene — the persistence request surface pulls
in no viewer, layout, or widget machinery. Real file I/O belongs in a **host**, not in `update`.

Be clear about what that host is today: **there isn't one.** No host in this org interprets a
`PersistenceEffect` into file I/O — `fs-gg-skiaviewer` scopes itself to window, render, and
screenshot I/O and documents no persistence sink. Nothing will pick these requests up unless you
write the backend yourself ([If you own the backend](#if-you-own-the-backend)).

### Which host interprets each `PersistenceEffect`

| `PersistenceEffect` | `Persistence.interpret` does | Which host runner interprets it |
|---|---|---|
| `Save` | records the request into `PersistenceEvidence.Requested` (clamping `Version` to `>= 0`); **writes no bytes** | **none** |
| `Load` | records the request; returns **no payload** | **none** |
| `DeleteSlot` | records the request; **deletes nothing** | **none** |

The "none" column is structural, not an oversight: a host runner interprets `ViewerEffect`, and **no
`ViewerEffect` case carries a `PersistenceEffect`**. A persistence request cannot reach a host runner
even in principle — `Persistence.interpret` is the only thing that will ever see it, and all it does
is hand the requests back to you.

So `PersistenceEvidence.Requested` proves that your `update` **asked** to save. It proves **nothing
about durability**, because no code in the framework writes a byte.

## Generated Product

Map each `Msg` that should save, load, or delete to a `PersistenceEffect` in your `update`, collect
the frame's requests, and pass them to `Persistence.interpret` for evidence today. The
request surface is designed so that a real backend can interpret the same values into file I/O
without changing it —
but no such backend exists yet, and handing a loaded save back to `update` as a `Msg` additionally
needs a result type the Canvas surface does not have (see
[If you own the backend](#if-you-own-the-backend)). Treat "the host will handle it later" as
*unbuilt work*, not as a delivered guarantee. The natural thing to snapshot is your seeded,
deterministic game-core `Model`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game-core]] — the simulation half of a game product; the seeded, deterministic `Model` is the natural thing to save.
- [[fs-gg-audio]] — the sibling requested-effect surface; persistence and audio are both effects requested from `update`.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map input to the `Msg` values whose `update` requests a save/load.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Effects-as-values / MVU background: https://guide.elm-lang.org/effects/
