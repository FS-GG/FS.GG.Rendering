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

## Public Contract

The signatures you consume are bundled with this product:

- `docs/api-surface/Canvas/Persistence.fsi` — the `PersistenceEffect` request DU
  (`Save`/`Load`/`DeleteSlot`), the `SaveSlot`/`SavePayload` identifiers, the `SaveEnvelope` record,
  the `PersistenceEvidence` record, and the `Persistence` module (smart constructors + the
  record-only `interpret`/`record`). Shipped in `FS.GG.UI.Canvas`, referenced on the `game` and
  `sample-pack` profiles (the same package that carries the simulation primitives and audio).

All helpers are **total**: the save-format version is clamped to `>= minVersion` at the boundary, and
no helper throws or performs I/O. The payload is **opaque** — the framework never parses it.

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
- **Expecting real save files in CI.** There is no file backend yet; the evidence is the *requested*
  values. Assert on `PersistenceEvidence.Requested`, not on files on disk.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned save/load examples (assert the
`PersistenceEvidence.Requested` sequence your `update` produces for a set of events).

## Evidence

Record persistence evidence (the requested-effect sequences for representative events — a checkpoint
save, a continue-game load, an erase-save delete) under this product's `readiness/` paths. Do not copy
framework readiness reports into the product.

## Package Boundary

`PersistenceEffect`/`Persistence` are in `FS.GG.UI.Canvas` (referenced only on the
`game`/`sample-pack` profiles). Canvas depends only on Scene — the persistence request surface pulls
in no viewer, layout, or widget machinery. Real file I/O belongs in the host (`fs-gg-skiaviewer`), not
in `update`.

## Generated Product

Map each `Msg` that should save, load, or delete to a `PersistenceEffect` in your `update`, collect
the frame's requests, and `Persistence.interpret` them for evidence today; when a real backend lands,
the host will interpret the same values into real file I/O — and hand a loaded save back to your
`update` as a `Msg` — with no change to your request surface. The natural thing to snapshot is your
seeded, deterministic game-core `Model`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the driven library's own reference), then
community sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-game-core]] — the simulation half of a game product; the seeded, deterministic `Model` is the natural thing to save.
- [[fs-gg-rendering:fs-gg-skiaviewer]] — the host window where a real file backend will interpret these requests and return loaded saves.
- [[fs-gg-audio]] — the sibling requested-effect surface; persistence and audio are both effects requested from `update`.
- [[fs-gg-rendering:fs-gg-keyboard-input]] — map input to the `Msg` values whose `update` requests a save/load.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Effects-as-values / MVU background: https://guide.elm-lang.org/effects/
