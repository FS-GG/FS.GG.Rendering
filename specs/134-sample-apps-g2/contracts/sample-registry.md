# Contract — Sample Registry (`SampleApps.Core`)

The pure surface the `Core` exposes to the `App` edge and the `Tests` project. No public *package* surface
(application-internal, FR-010). This is the seam the Expecto suites bind to — the G2 analogue of G1's
`page-registry.md`.

## `Registry.all : SampleEntry list`

Exactly six entries, in a stable order. Each `SampleEntry` (see data-model.md) is **non-generic** — its
sample-specific `Model`/`Msg` are erased behind closures (research R2):

```fsharp
type SampleEntry =
    { Id: string
      Family: string                          // "game" | "productivity"
      Title: string
      Controls: string list                   // ⊆ Catalog.supportedControls
      Inputs: string list                     // ⊆ { "keyboard"; "pointer"; "timing-step" }
      RunEvidence: int -> string -> SampleEvidenceRecord
      Interactive: ThemeMode -> int
      Outcome: ExpectedOutcome }
```

### Registry invariants (asserted by `CoverageBacklogTests` / `BuildOutcomeTests`)

1. `Registry.all` has length 6; `Id`s are unique and ∈ `{tetris,snake,pong,kanban,todo,calendar}`.
2. Families: exactly 3 `"game"` + 3 `"productivity"`.
3. Every `Controls` id ∈ `Catalog.supportedControls` (no dangling control reference).
4. `⋃ Inputs` over all entries ⊇ `{ "keyboard"; "pointer"; "timing-step" }` (FR-011/SC-004).
5. Each `Outcome` is non-empty and is the value the run is checked against (FR-009/SC-001).

## `Harness` (shared builders)

- `host : init -> update -> view -> mapKey -> tick -> theme -> InteractiveAppHost<'M,'Msg>` — the bridge
  every sample uses (G1's `Host.create` generalized; **`tick` is non-None for games**).
- `evidenceFor : InteractiveAppHost<'M,'Msg> -> FrameInput<'Msg> list -> ('M -> ExpectedOutcome) ->
   ExpectedOutcome -> int -> string -> SampleEvidenceRecord` — replays the script via `Perf.runScript`,
  derives the achieved outcome from the final model, compares to the authored `ExpectedOutcome`, captures
  the (optional) screenshot, and writes the record. One implementation, reused by all six `RunEvidence`
  closures.

## Per-sample module shape

Each `Games/<X>.fs` and `Productivity/<X>.fs` exposes (application-internal):
`Model`, `Msg`, `init : Prng -> Model` (or `init : Model` for productivity), `update : Msg -> Model ->
Model`, `view : Size -> Model -> Control<Msg>`, `mapKey`, `tick`, `script : FrameInput<Msg> list`,
`expected : ExpectedOutcome`, and `entry : SampleEntry` (closes over all the above via `Harness`).

## Coverage surface

- `Coverage.coverageRows : CoverageRow list` — one row per curated sample.
- `Coverage.backlog : BacklogEntry list` — all **22** archived specs, each `Adopted`/`Deferred` + reason.
- `Coverage.check : unit -> CoverageBacklogResult` — runs every invariant in `coverage-backlog.md`;
  `Coverage.render : unit -> string` — the committed report text. (G1's `CoverageMap.check`/`summary`
  analogue.)
