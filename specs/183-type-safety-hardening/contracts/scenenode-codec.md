# Contract: `SceneNode` Codec Symmetry + DU Normalization (US2 / FR-002, FR-003) — Tier 1, bump Scene

## Invariant

(a) The `SceneNode` write/read codec is driven by one per-case **table** so a missing case is caught at
build/test time, not silently mis-serialized. (b) The 19 bare-tuple `SceneNode` cases gain **named
fields preserving exact arity and types** (source-compatible). The binary **wire format is frozen**.

## Must hold

1. **Wire format byte-frozen.** Tags **0–24** (sequential DU order), field order, and every primitive
   encoding are unchanged. The codec-byte corpus (one value per case) serializes to **byte-identical**
   output vs baseline (behavior-invariance §A.1). This is the hard gate — replay/persisted caches depend
   on it.
2. **Round-trip identity for all 25 cases.** An every-case round-trip test constructs one value of each
   case, asserts `deserialize (serialize x) = x`, and asserts the bytes equal the captured baseline.
3. **Symmetry enforced.**
   - Write stays an exhaustive `match node` (tag + payload); `FS0025`-as-error makes a missing case a
     **compile** error.
   - The codec table has exactly 25 rows with contiguous tags `0..24` (asserted by test).
   - Read dispatches via the tag→reader table; the `| tag -> failwithf` wildcard remains **only** for
     genuinely-corrupt/unknown tags, never as a substitute for a missing case.
   - Net effect: adding a `SceneNode` case forces a write arm (compile) + a table row + a round-trip-list
     entry (test) — "compile + test enforced" (FR-002).
4. **DU normalization is source-compatible.** Named fields preserve arity/types (see
   [data-model.md](../data-model.md) §3); positional construction and matching across `src/`, samples,
   template, and generated products still compile — only `Scene.fs`/`Scene.fsi` are edited for the DU
   itself. The 6 already-named cases are untouched.
5. **Surface diff is exactly the field names.** `Scene.fsi` git diff shows only the DU field-name edits
   (and US3's `damageRegion` if landed together); `FS.GG.UI.Scene.txt` shows no unplanned type changes.
   `FS.GG.UI.Scene` is version-bumped (shared with US3).
6. **Incidental cleanup (optional, no wire change).** The 3 `writeXOption`/`readXOption` near-clones may
   fold into generic `writeOption`/`readOption`; if that perturbs any byte, revert it (byte-stability wins).

## Explicitly NOT in this story (FR-010 / Out of Scope)

- **Flattening** inner tuples (`Rectangle of x*y*w*h*fill`) or **retyping** `(float*float*float*float)`→
  `Rect` / `(float*float)`→`Point`. That is an arity/type change breaking construction sites and risking
  wire/behavior drift — deferred as a possible future feature, recorded with rationale.
- Any change to tag values, field order, or primitive encodings (wire-format change — out of scope).

## Retain-per-FR-010 triggers

If naming a particular case's fields cannot be done without changing arity (it can't, for these 19) or
if the table refactor perturbs any wire byte that can't be reconciled, retain that case's hand-codec and
record why.
