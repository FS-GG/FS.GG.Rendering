// See skill: fs-gg-symbology
namespace FS.GG.UI.Symbology

/// The machine-readable **element↔visual CATALOG** format — the single source of truth that pairs the
/// two halves of the visual-completeness contract (#990 authors it; #989's `Coverage` checks it). A
/// catalog is a flat, ordered list of ROWS, one per gameplay element (doors, bombs, explosions,
/// projectiles, enemies, terrain, pickups, hazards, status markers — every element that is, in
/// principle, visible). Each row records the element's APPROVED VISUAL: either a `Shown` token handle
/// (a visible representation) or a `Hidden` opt-out naming the gameplay mechanic that suppresses it.
///
/// This module owns the FORMAT and the deterministic (de)serialization, plus the bridge that hands a
/// catalog to `Coverage.check` unchanged — so "the artifact `fs-gg-symbol-design` maintains" and "the
/// artifact `Coverage` consumes" are provably the SAME thing. Like `Coverage` and `Legibility`, it is
/// generic and product-agnostic: a game's element ids and token handles are its OWN strings; this module
/// never owns the per-game list. Deterministic; no wall-clock, randomness, or IO — `render` of an
/// unchanged catalog is byte-identical, and `parse` of an equal artifact yields an equal value.
///
/// Separation of concerns with `Coverage`: the catalog carries a token *handle* (a stable name into the
/// product's symbol-set module / ChannelMap), NOT inlined `Token` geometry — the actual approved
/// `Token` lives in code, referenced by the handle. `Coverage` asks the presence question only
/// (shown / reasoned-hidden / missing), so the bridge witnesses a `Shown` handle with
/// `Symbology.defaultToken`; the real token is resolved by the renderer, never by the coverage check.
[<RequireQualifiedAccess>]
module Catalog =

    /// A gameplay element's approved visual disposition in the persisted catalog — the machine-readable
    /// mirror of `Coverage.Representation`, but PORTABLE. `Shown` names the approved token by a stable
    /// HANDLE (a symbol/token id in the product's symbol-set module), never inlined geometry. `Hidden`
    /// is a deliberate, reasoned opt-out whose string names the MECHANIC that suppresses the visual (fog
    /// of war, stealth, an off-screen or purely-internal marker). There is deliberately no third
    /// "nothing" case: a forgotten element is an element with NO row, surfaced by `coverage` as `Missing`
    /// rather than encoded as a value.
    type Visual =
        | Shown of token: string
        | Hidden of reason: string

    /// One catalog row: a gameplay element (a stable id — the element's DU-case name is the canonical
    /// choice) paired with its approved `Visual`.
    type Entry =
        { Element: string
          Visual: Visual }

    /// The whole element↔visual catalog: rows in DECLARED order (the order the elements are enumerated /
    /// authored). Order is significant and preserved by `render`/`parse`, so a re-render is a byte
    /// comparison and a diff shows exactly which element changed disposition.
    type Catalog = { Entries: Entry list }

    /// Why the independent inventory/catalog/runtime-binding audit failed.
    [<RequireQualifiedAccess>]
    type BindingGap =
        | EmptyInventory
        | DuplicateDeclared
        | Missing
        | Stale
        | Unbound
        | Unobserved
        | UnsupportedHidden

    /// One independently derived visual binding problem.
    type BindingFinding =
        { Element: string
          Gap: BindingGap
          Message: string }

    type BindingVerdict =
        | Complete
        | Incomplete

    /// Digests computed by the caller from the exact inventory declaration, catalog bytes, and
    /// representative runtime-render evidence that produced the mechanical report.
    type EvidenceDigests =
        { Inventory: string
          Catalog: string
          Render: string }

    /// Mechanical coverage kept separate from the independent critic. `Complete` means the runtime
    /// inventory is non-empty and unique, catalog rows exactly match it, shown handles resolve through
    /// the runtime registry and were observed through representative runtime rendering, and every
    /// hidden row uses the explicit `<mechanic>: <explanation>` form.
    type BindingReport =
        { DeclaredElements: string list
          EvidenceDigests: EvidenceDigests
          Findings: BindingFinding list
          OptedOut: (string * string) list
          Verdict: BindingVerdict }

    /// Parse the canonical text form back to a catalog. Returns `Error message` on a malformed artifact:
    /// a missing/wrong version header, a row without a `shown`/`hidden` disposition, a `shown` row with a
    /// blank token handle (a shown-as-nothing row is malformed), or a duplicate element id. A `hidden`
    /// row with a blank reason PARSES (structure is well-formed); its reason-quality is `validate`'s
    /// business (`Unreasoned`), keeping FORMAT and POLICY separate. Blank lines and `#` comment lines
    /// after the header are ignored. `parse (render c) = Ok c` for every well-formed `c`.
    val parse: text: string -> Result<Catalog, string>

    /// Serialize a catalog to its canonical, deterministic text form — a versioned header line
    /// (`# fs-gg element-visual catalog v1`) followed by one tab-separated row per entry, in order:
    /// `element<TAB>shown<TAB>handle` or `element<TAB>hidden<TAB>reason`. Byte-identical for an unchanged
    /// catalog; no clock, no randomness. Assumes a well-formed catalog (ids/handles free of tab and
    /// newline — the constraint `parse` enforces).
    val render: catalog: Catalog -> string

    /// The visual disposition recorded for `element`, or `None` if the catalog has no row for it (a
    /// forgotten element — the silent-omission `coverage` reports as `Missing`). First matching row wins.
    val tryFind: element: string -> catalog: Catalog -> Visual option

    /// Bridge one catalog `Visual` to a `Coverage.Representation`. `Shown handle` becomes
    /// `Coverage.Shown Symbology.defaultToken` — a WITNESS: coverage is presence-only, so the token
    /// carried is a placeholder and the real approved token is resolved from `handle` by the renderer.
    /// `Hidden reason` maps straight through to `Coverage.Hidden reason` (a blank reason therefore
    /// surfaces as `Coverage.Unreasoned`, exactly as #989 specifies).
    val toRepresentation: visual: Visual -> Coverage.Representation

    /// The element ids the catalog declares, in row order — the "renderable-element set" `Coverage.check`
    /// runs against when the catalog is checked against ITSELF (`validate`).
    val declaredElements: catalog: Catalog -> string list

    /// The coverage check over a catalog: given a product's DECLARED renderable-element set (its element
    /// ids — typically its DU-case names) and the catalog, report every declared element that the catalog
    /// forgot (no row -> `Missing`) or opted out with a blank reason (`Unreasoned`). This is exactly
    /// `Coverage.check declared (fun e -> tryFind e catalog |> Option.map toRepresentation)` — the SAME
    /// enforcement #989 ships, sourced from the machine-readable artifact rather than a hand-written map.
    /// `Verdict = Covered` iff every declared element has a `Shown` row or a `Hidden` row with a non-blank
    /// reason. This is the intake #994's scaffold-emitted gate consumes.
    val coverage: declared: string list -> catalog: Catalog -> Coverage.Report<string>

    /// Self-consistency check: `coverage (declaredElements catalog) catalog`. Since every row is `Shown`
    /// or `Hidden`, this can only surface `Unreasoned` findings (a `Hidden` row with a blank reason) — it
    /// proves the catalog ARTIFACT is itself well-formed, independent of any product's DU. `Covered` iff
    /// no opt-out is blank-reasoned.
    val validate: catalog: Catalog -> Coverage.Report<string>

    /// Compare a runtime-owned gameplay inventory to the catalog, then require every shown handle to
    /// resolve through the runtime visual registry and appear in representative runtime rendering.
    /// The catalog never supplies its own subject set to this check.
    val audit:
        declared: string list ->
        catalog: Catalog ->
        registeredBindings: (string * string) list ->
        observedBindings: (string * string) list ->
        evidenceDigests: EvidenceDigests ->
            BindingReport
