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
Real file I/O is a **host** concern: the framework carries your requests to a host and carries the
answers back (`Viewer.runAppWithPersistence`), but **the sink that writes the bytes is code you
write** — no file backend ships in this org. This skill covers requesting save/load, proving what was
requested, and reaching a real host with it. It materializes for the `game` and `sample-pack` profiles.

> **Building the file backend rather than consuming it?** Then the testing guidance below is written
> for the wrong reader, and following it will hand you a suite that passes against a backend that
> writes nothing. Read [If you own the backend](#if-you-own-the-backend) first.

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Canvas/Persistence.fsi` — the `PersistenceEffect` request DU
  (`Save`/`Load`/`DeleteSlot`), the `SaveSlot`/`SavePayload` identifiers, the `SaveEnvelope` record,
  the `PersistenceEvidence` record, and the `Persistence` module (smart constructors + the
  record-only `interpretRecordOnly`/`record`). Shipped in `FS.GG.UI.Canvas`, referenced on the
  `game` and `sample-pack` profiles. That package carries **only** the pure elements, the render loop,
  and this persistence request surface — the pinned Canvas api-surface is exactly `Elements.fsi` and
  `Persistence.fsi`. It carries **no audio** (that vocabulary was retired at the Canvas `0.3.0` major;
  audio is `FS.GG.Audio.Core`/`FS.GG.Audio.Host`, a separate package your product pins itself) and
  **no simulation primitives** (those are `FS.GG.Game.Core`). [[fs-gg-game:fs-gg-audio]] says the same thing from
  its side; if the two ever disagree, the api-surface bundled with your product settles it.

All helpers are **total**: the save-format version is clamped to `>= minVersion` at the boundary, and
no helper throws or performs I/O. The payload is **opaque** — the framework never parses it.

> **`Persistence.interpretRecordOnly` PERSISTS NOTHING.** It records your requests into
> `PersistenceEvidence.Requested` and drops them. No file is written, read or deleted. The name says
> exactly what it does: it is a **record-only fold**, the thing you assert on in a test, and a product
> whose only sink is this one has saved nothing.
>
> **That is not the same as saying the requests have nowhere to go — they do now.**
> `ViewerEffect.Persist` carries a batch of `PersistenceEffect` to a host, and
> `Viewer.runAppWithPersistence` hands that batch to a **sink you supply**, then dispatches each
> `PersistenceOutcome` the sink returns back into your `update` as a message. So a `Load` is
> answerable and a `Save` can really reach the disk — through code **you** write. See
> [Reaching a real host](#reaching-a-real-host).
>
> The two are not rivals, and knowing which one you are standing in front of is the whole skill:
> **`interpretRecordOnly` is what you assert on; the sink is what actually writes.** Requests are not
> effects, and the fold that records them is not the host that performs them.

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
    | ContinueGame      -> Persistence.load (SaveSlot "slot-1")     // answer arrives as a Msg, via mapOutcome
    | EraseSave         -> Persistence.deleteSlot (SaveSlot "slot-1")
```

`SaveSlot` is an opaque name **you** own — the framework does not map it to a path. The **version**
you stamp lets a future load migrate or reject an old save; that migration policy is yours, in your
own deserialize step.

## Deciding *when* to save: the cue seam, and the `Init` blind spot

Products decide their save/load requests in **one place**, by analogy with `AudioCues.forTransition`
([[fs-gg-game:fs-gg-audio]]): a `SaveCues.forTransition : Msg -> Model -> Model -> PersistenceEffect list` that
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
**Persistence has no scaffold-wired equivalent.** A host that *performs* your requests does exist
(`Viewer.runAppWithPersistence` — see [Reaching a real host](#reaching-a-real-host)), but nothing in
the framework calls a `SaveCues` of **yours**: the framework carries the effects you hand it, and an
effect nobody constructed is an effect it never sees. The seam, the `Started` case, and the call from
your own `Init` are **all product-owned**:

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

(The `Load` answer comes back as a **message**, not as a return value — `Viewer.runAppWithPersistence`
dispatches each `PersistenceOutcome` into `update` — so it still cannot arrive *before* frame 0, and
under `interpretRecordOnly` it never arrives at all. A product that genuinely needs restored state at
frame 0 still builds it in `initialModel`; `Started` is then where it *declares* what it did, so the
sink hears about it and a test can prove it.)

**Assert at the sink, not at the model.** The only test shape that catches this class asks what the
sink was *told*, not what the model *holds* — [[fs-gg-rendering:fs-gg-testing]] works the case end to
end. Under `interpretRecordOnly` the "sink" is the record-only fold, so that means asserting
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

`Persistence.interpretRecordOnly` folds a batch of requests into `PersistenceEvidence` — the requested
effects in dispatch order, with `Save` versions normalized and payloads carried verbatim. This is the
record-only interpreter: it never blocks, never touches the filesystem, and is the evidence you
assert on in tests. `Persistence.record` appends a single effect if you accumulate frame by frame.

```fsharp
open FS.GG.UI.Canvas

let evidence =
    Persistence.interpretRecordOnly
        [ Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") "{score:42}")
          Persistence.load (SaveSlot "slot-1")
          Persistence.deleteSlot (SaveSlot "old") ]

// evidence.Requested =
//   [ Save { Version = 1; Slot = SaveSlot "slot-1"; Payload = SavePayload "{score:42}" }
//     Load (SaveSlot "slot-1"); DeleteSlot (SaveSlot "old") ]
```

## Common pitfalls

- **Reading/writing a file inside `update`.** `update` must stay pure — return a `PersistenceEffect`
  value and let the host — or, in a test, the record-only fold — act on it. I/O in `update` breaks
  determinism and testability.
- **Expecting `Load` to return the save synchronously.** The pure surface only *requests* a load. Under
  `Viewer.runAppWithPersistence` the answer comes back as a `Msg` your `update` handles (your
  `mapOutcome` turns the `PersistenceOutcome` into one); under `interpretRecordOnly` the request is
  merely recorded and nothing ever answers it. Either way, it is never a return value.
- **Launching under a runner that does not carry persistence.** Only `Viewer.runAppWithPersistence` and
  `Viewer.runAppWithAudioAndPersistence` interpret `ViewerEffect.Persist`. **Every other runner discards
  it** — `Viewer.runApp`, `Viewer.runAppWithAudio`, the `WindowBehavior` forms, the interactive-viewer
  family. They say so on the diagnostics channel rather than dropping it in silence, but they write
  nothing: a product that requests saves and launches with any other runner loses every one of them,
  while the model looks perfectly correct throughout. The list of runners that persist is an
  **allow-list of two** — check yours against it.
- **Letting the framework own your format.** `SavePayload` is opaque — serialize and version your
  `Model` yourself. The framework carries the bytes verbatim and never parses them; a schema change is
  your deserialize step's job, which is why you stamp the version.
- **Expecting `Load`/`DeleteSlot` of an empty slot to error.** The interpreter records exactly what
  you request; "no such save" is your sink's concern, and it answers `PersistenceOutcome.Absent` — not
  an exception here.
- **Expecting a save restored in `initialModel` to have *asked* for anything.** The initial model
  makes no *transition*, so `forTransition` never runs for it and every request the loaded state
  implies is silently never made — while the model is perfectly correct and a test that asserts on it
  **passes**. This is the main path for persistence, not an edge case. Handle it under `Started`; see
  [Deciding *when* to save](#deciding-when-to-save-the-cue-seam-and-the-init-blind-spot).
- **Expecting real save files from `interpretRecordOnly`.** The record-only fold is not a backend, so
  under it the evidence is the *requested* values: assert on `PersistenceEvidence.Requested`, not on
  files on disk. **If you are building the backend, invert this** — a `Requested`-only suite passes
  against a sink that writes nothing. See [If you own the backend](#if-you-own-the-backend).

## If you own the backend

Everything above is written for a product that *consumes* a backend somebody else wrote. **If your work
item is to build the file backend itself, the guidance above describes the trap, not the target.**

`Persistence.interpretRecordOnly`/`record` are **record-only**: they fold requests into
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

⚠️ **The answer half of the vocabulary has LANDED — report in it, and do not invent your own.**
`PersistenceEffect` is request-only (`Save`/`Load`/`DeleteSlot`), so a product that asked for a `Load`
once had nowhere to receive the answer: it could be asked and never answered, and nothing said so.
The `FS.GG.UI.Canvas` this product pins closes that hole with `PersistenceOutcome` — what a host
reports back *after actually performing* a request:

```fsharp
open FS.GG.UI.Canvas

// The words YOUR backend answers in. Every case is a distinct thing that really happened.
let describe (outcome: PersistenceOutcome) : string =
    match outcome with
    | PersistenceOutcome.Saved slot -> $"written and durable: {slot}"
    | PersistenceOutcome.Loaded envelope -> $"read back v{envelope.Version} from {envelope.Slot}"
    | PersistenceOutcome.Deleted slot -> $"deleted (idempotent): {slot}"
    // A NORMAL answer, not a failure: a new player has no save. Start fresh.
    | PersistenceOutcome.Absent slot -> $"no save in {slot} — start fresh"
    // DATA LOSS: bytes were there and are unusable. Tell the player; do NOT silently overwrite.
    | PersistenceOutcome.Unreadable (slot, reason) -> $"CORRUPT save in {slot}: {reason}"
    // The request never completed at all — disk full, location not writable, no permission.
    | PersistenceOutcome.Failed (_, reason) -> $"request never completed: {reason}"
```

**`Absent` and `Unreadable` are deliberately distinct, and collapsing them is the bug this type exists
to prevent** — the same bug the bullet above warns you about, now guarded by the type system rather
than by your memory. A single `LoadFailed` case lets a corrupt save be reported as a new game, and the
next autosave then overwrites the bytes the player was about to lose.

**Nothing in the framework *invents* a `PersistenceOutcome` — your sink does.**
`Persistence.interpretRecordOnly` cannot produce one: it writes no bytes, so it has nothing to report.
What the framework now does is **carry** them. `Viewer.runAppWithPersistence` takes your sink —
`PersistenceEffect list -> PersistenceOutcome list` — and hands each outcome it returns to your
`mapOutcome`, dispatching into `update` the ones you map to `Some` (an outcome you map to `None` is
dropped). So the words in the list above are the words **you answer in**, and the framework is the
thing that delivers them: you are not inventing a private result type, and two products' backends
answer in the same vocabulary.

## Reaching a real host

Everything above the record-only fold is testable without a host. **Durability is not** — and the
dispatch path that gets you there is a runner you have to choose on purpose.

- `ViewerEffect.Persist` carries a `PersistenceEffect list` to the host. It is the only case that does.
- **Exactly two runners interpret it**: `Viewer.runAppWithPersistence` and
  `Viewer.runAppWithAudioAndPersistence`. They hand the batch to your `persistenceSink`
  (`PersistenceEffect list -> PersistenceOutcome list`), then pass each answer to your `mapOutcome`
  (`PersistenceOutcome -> 'msg option`) and dispatch the ones it maps to `Some` into `update`. An
  outcome you map to `None` is **dropped** — the host still answered; you chose not to listen. The
  paired audio form exists so you do not have to trade sound for saves.
- **Every other runner discards `ViewerEffect.Persist`** — `Viewer.runApp`, `Viewer.runAppWithAudio`,
  the `WindowBehavior` forms, the `runInteractiveViewer` family, all of them. They report the drop on
  the diagnostics channel rather than swallowing it in silence, but they perform nothing. **Read that
  as an allow-list, not a deny-list:** if the runner you launched is not one of the two named above,
  your saves are being thrown away.

**The sink is still yours to write.** No file backend ships in this org: the framework gives you the
request vocabulary, the answer vocabulary, and the dispatch path between them — the bytes are your
code. Test it as [If you own the backend](#if-you-own-the-backend) says, on the files, not the requests.

Two hazards in the seam, both of which cost real products real bugs:

- ⚠️ **The sink runs on the window thread.** It is called from the effect fold that the tick and key
  handlers drive, so **a slow save stalls the frame** for as long as the write takes. Unlike the audio
  sink (fire-and-forget, `-> unit`), this one must return an answer, so it cannot simply be made async.
  Keep it fast, or hand the work to your own worker and answer later with a message.
- 🛑 **Never request a persistence effect from the handler for a `PersistenceOutcome`.** The dispatch is
  **synchronous recursion**, not the Elmish dispatch queue: an outcome whose handler requests another
  save recurses on one stack and terminates in a `StackOverflowException` — which .NET **cannot catch**.
  The process dies with no diagnostic at all. If an outcome must trigger further work, record the
  intent in your `Model` and let the next transition request it.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned save/load examples. To prove what your `update`
*requested*, assert the `PersistenceEvidence.Requested` sequence it produces for a set of events.
**If you own a file backend, assert on the files it wrote instead** — see
[If you own the backend](#if-you-own-the-backend).

## Evidence

Record persistence evidence (the requested-effect sequences for representative events — a checkpoint
save, a continue-game load, an erase-save delete) under this product's `readiness/` paths. Do not copy
framework readiness reports into the product.

## Package Boundary

`PersistenceEffect`/`Persistence` are in `FS.GG.UI.Canvas` (referenced only on the
`game`/`sample-pack` profiles). Canvas depends only on Scene — the persistence request surface pulls
in no viewer, layout, or widget machinery. Real file I/O belongs in a **host**, not in `update`.

Be clear about what that host is: **a runner you choose, driving a sink you wrote.**
`Viewer.runAppWithPersistence` (and `Viewer.runAppWithAudioAndPersistence`) interpret
`ViewerEffect.Persist` by calling *your* `persistenceSink` and dispatching its `PersistenceOutcome`s
back into `update`. What the org does **not** ship is the sink itself — no file backend exists here, so
the bytes are still yours ([If you own the backend](#if-you-own-the-backend)).

### Which host interprets each `PersistenceEffect`

| `PersistenceEffect` | `Persistence.interpretRecordOnly` does | Which host runner interprets it |
|---|---|---|
| `Save` | records the request into `PersistenceEvidence.Requested` (clamping `Version` to `>= 0`); **writes no bytes** | `Viewer.runAppWithPersistence` / `Viewer.runAppWithAudioAndPersistence` — via **your** sink |
| `Load` | records the request; returns **no payload** | as above; the answer returns as a `PersistenceOutcome` dispatched into `update` |
| `DeleteSlot` | records the request; **deletes nothing** | as above |
| *(any of them)* | — | **every other runner** (`Viewer.runApp`, `Viewer.runAppWithAudio`, the `WindowBehavior` and interactive-viewer forms) — **discarded**, reported on the diagnostics channel |

The last row is the one that bites, and it is an **allow-list of two**. A host runner interprets
`ViewerEffect`, and `ViewerEffect.Persist` is the case that carries persistence — so the request
reaches a host **only** under a runner that knows about it. Launch the same product under any other
runner and every save is dropped.

So `PersistenceEvidence.Requested` proves that your `update` **asked** to save. It proves **nothing
about durability**: no code in the *framework* writes a byte, and whether anything does depends on the
runner you launched and the sink you supplied.

## Generated Product

Map each `Msg` that should save, load, or delete to a `PersistenceEffect` in your `update`, collect
the frame's requests, and pass them to `Persistence.interpretRecordOnly` for the evidence your tests
assert on. The same values are what a real backend interprets into file I/O — the request surface does
not change between the two.

The pinned surface gives you three of the four pieces: the *request* vocabulary (`PersistenceEffect`),
the *answer* vocabulary (`PersistenceOutcome`), and the **dispatch path** between them
(`ViewerEffect.Persist`, carried by `Viewer.runAppWithPersistence`, which maps each outcome back into
your `update` as a `Msg` — see [Reaching a real host](#reaching-a-real-host)).

**The fourth piece is the sink, and it is yours.** No file backend ships in this org, so "the host will
handle it" is only true of the *plumbing*: nothing writes a byte until you write the code that does.
Treat the backend as *unbuilt work*, not as a delivered guarantee — and remember that a product
launched under `Viewer.runApp` has no persistence at all, however many effects it requests. The natural
thing to snapshot is your seeded, deterministic game-core `Model`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game:fs-gg-game-core]] — the simulation half of a game product; the seeded, deterministic `Model` is the natural thing to save.
- [[fs-gg-game:fs-gg-audio]] — the sibling requested-effect surface; persistence and audio are both effects requested from `update`.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map input to the `Msg` values whose `update` requests a save/load.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Effects-as-values / MVU background: https://guide.elm-lang.org/effects/
