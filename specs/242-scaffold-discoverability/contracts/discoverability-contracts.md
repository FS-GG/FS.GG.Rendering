# Contracts: Scaffold discoverability sharpening (spec 242)

Two consumer-facing surfaces. Neither is a versioned cross-repo contract (FR-010); both are generated-product surfaces exercised by the governance/template gates.

## Contract A — Build-entry help (CLI)

**Surface**: `dotnet fsi build.fsx --help` · `dotnet fsi build.fsx -h` · `dotnet fsi build.fsx help` · `./fake.sh build --help` · `./build.sh --help` · `./build.sh -h`

**Request**: any of the above help tokens present anywhere in the args.

**Response** (stdout): a banner that
- lists the available targets, and
- states, for `Dev`/`Test`/`Verify`, the load-bearing semantics from `data-model.md` (Dev does not compile; Test is the first real compile+test; Verify's audit hard-blocks until every task is `[X]`).

**Guarantees**:
- Exit code `0`.
- No `readiness/logs/<target>.txt` written (help is side-effect-free).
- No build target executed (frozen `Test`/`Verify` bodies not entered).

**Non-goals**: help does not resolve the engine assembly, restore packages, or touch `Directory.Packages.props`.

## Contract B — Generated `SWAP-CHECKLIST.md`

**Surface**: a `SWAP-CHECKLIST.md` file at the generated product root, present for every `profile`.

**Guarantees**:
- Content is correct for the instantiated `profile`'s model family (game / app+sample-pack / governed+headless-scene) per `data-model.md`.
- Names every durable re-point file (`LayoutEvidence.fs`, `EvidenceCommands.fs`) and the replaceable files (`Model.fs`, `View.fs`, `BehaviorTests.fs`), and references `docs/scaffold-map.md`.
- Every symbol it names exists in the generated tree (no phantoms).
- It is **advisory** developer content: no gate pins its exact per-symbol prose, so a model swap may rewrite it freely (only presence + structure is asserted in the product).

## Contract C — Template emission (`.template.config/template.json`)

**Surface**: new profile-gated `sources[]` entries re-emitting the family-correct `SWAP-CHECKLIST.md` from `template/fragments/swap-checklist/<family>/`, following the `docs/skillist-reference.md` exclude-and-re-emit precedent.

**Guarantees**:
- The three family conditions are mutually exclusive and exhaustive over the five profiles, so exactly one `SWAP-CHECKLIST.md` lands per instantiation.
- `copyOnly` (verbatim, no `sourceName` substitution) so the checklist's file/symbol names are not rewritten by `Product` substitution.
- The unconditional base source and the six-scanned-file order are unchanged.

## Contract D — Sync gate (`docs/product.md` ↔ banner)

**Surface**: the load-bearing `Dev`/`Test`/`Verify` phrases.

**Guarantee**: the phrases in the `build.fsx` banner and in `docs/product.md` agree; either drifting fails `GovernanceTests.fs`.
