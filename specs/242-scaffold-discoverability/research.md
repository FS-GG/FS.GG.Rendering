# Research: Scaffold discoverability sharpening (spec 242)

Phase 0 — resolve the mechanical unknowns behind the two additive asks (SWAP-CHECKLIST.md, build-target `--help` banner). Evidence gathered from `.template.config/template.json`, `template/base/build.fsx`, `template/base/build.sh`, `template/base/src/Product/*.fs`, `template/base/tests/Product.Tests/GovernanceTests.fs`, and the per-profile validation harness under `scripts/` + `tests/Package.Tests/`.

## Decision 1 — How to emit a per-profile `SWAP-CHECKLIST.md`

**Decision**: Ship `SWAP-CHECKLIST.md` via **separate, profile-gated `sources[]` entries** in `.template.config/template.json`, each copying a profile-family-correct authored file to the product root — mirroring the proven `docs/skillist-reference.md` precedent (excluded from the ungated base source, re-emitted by a condition-gated source). Group into three model families:

- **game** → the Pong starter model.
- **app family** (`app`, `sample-pack`) → the app/controls demo model (sample-pack reuses the app model).
- **governed family** (`governed`, `headless-scene`) → the minimal `Name`/`RenderCount` scene model.

**Rationale**:
- The value of §2.2 is a *precise, product-specific* to-do list. A single union doc listing all profiles' symbols reintroduces the "which applies to me" friction the item exists to remove, so per-profile content is load-bearing, not cosmetic.
- Inline `//#if` preprocessing is only *proven* for `.fs`/`.fsx` (the engine's C-style `//` comment handler). **No `.md` file in the repo uses `#if`, and there is no markdown comment-syntax config** in `template.json`. Betting a shipped consumer artifact on the engine's unverified default markdown handler risks emitting literal `//#if` lines into products — unacceptable for an "additive, low-risk" edge-sharpening item.
- The separate-source pattern is exactly how the repo already ships whole-file per-profile content (skills at `template.json` skill sources, `samples` from `template/fragments/samples/`, and `skillist-reference.md` re-emitted under a `lifecycle` condition). Profile families are mutually exclusive, so the three sources never both target `./SWAP-CHECKLIST.md` for the same instantiation.

**Alternatives considered**:
- *Inline `#if` in one markdown file* — simplest source-wise but unproven for `.md`; rejected (risk of shipping raw conditional markers).
- *Dynamically generate the checklist at build time by parsing `src/Product/*.fs`* — rejected: heavy (F# parsing), and the consumer needs it present immediately post-scaffold, before any build.
- *One shared profile-agnostic doc in `template/base/`* (product.md precedent) — rejected for the checklist because precision per profile is the point (though this precedent IS reused for the build banner, Decision 2).

## Decision 2 — Build-target `--help` banner

**Decision**: Add a help path to `template/base/build.fsx` that recognizes `--help` / `-h` / `help` in the args **before** target resolution, prints a banner enumerating the targets and the load-bearing `Dev`/`Test`/`Verify` semantics, and **exits 0 without writing any `readiness/logs/<target>.txt`**. Mirror it at the shell layer in `template/base/build.sh` (extend `print_usage` into a semantics banner and add a `--help`/`-h` verb) so `./build.sh --help` and `dotnet fsi build.fsx --help` (via `fake.sh`) both surface it.

**Rationale**:
- The semantics are identical across all profiles, so the banner needs **no** profile conditional — it ships in the unconditional base `build.fsx`/`build.sh` for every profile (like `product.md`).
- `build.fsx` already routes args through `targetFromArgs` (`build.fsx:24-33`) into `run` (`build.fsx:227-261`); a help branch is an additive early-return, touching neither the frozen `Test`/`Verify` bodies nor the engine-resolution path.
- FR-008 (no side effects on help) is satisfiable because `writeLog` is only reached inside `run`; the help path returns before dispatch.

**Alternatives considered**:
- *Only edit `build.sh`* — rejected: the consumer's confusion came from `./fake.sh build -t Dev` (the `.fsx` path); the banner must live where the `.fsx` entry sees a help request, not only in the shell wrapper.
- *A separate `docs/build-targets.md`* — rejected: the item explicitly wants the semantics at the build *entry point* ("someone running the build sees them"), not another doc to discover.

## Decision 3 — Keeping the banner in sync with `docs/product.md` (FR-009)

**Decision**: A **text-consistency gate** in the durable `GovernanceTests.fs` (which already reads `build.fsx`): assert the load-bearing semantic phrases (e.g. `Dev` "does not compile" / completion-marker, `Test` "first real … dotnet test", `Verify` "merge-gate audit" that "hard-blocks until every task is [X]") appear **both** in the `build.fsx` banner string **and** in `docs/product.md`. Drift in either direction fails the gate.

**Rationale**: Cheap, deterministic, and directly enforces SC-004. `product.md` stays the prose home; the banner is a checked projection. `GovernanceTests.fs` is model-agnostic and template-authored, so this assertion survives any model swap.

**Alternative considered**: Generate the banner text *from* `product.md` at build time — rejected as over-engineered; a shared-phrase assertion catches drift without a codegen step.

## Decision 4 — Where the two governance layers live

**Decision**:
- **Generated-product presence/discoverability** (FR-001, FR-005) → extend `template/base/tests/Product.Tests/GovernanceTests.fs` (both the governed branch and the app/game `//#else` branch): assert `SWAP-CHECKLIST.md` exists at the product root and names the re-point files + points to `docs/scaffold-map.md`. Assert **presence and structure only**, never exact per-symbol prose, so a legitimate model swap can freely edit the checklist without failing the gate (FR-005 / edge case).
- **Template-authoring correctness** (SC-002) → a new deterministic test under `tests/Package.Tests/` that reads each shipped `SWAP-CHECKLIST.md` variant **and** the corresponding `template/base/src/Product/*.fs` profile branch, asserting every symbol the checklist names exists in that source (no phantoms) and the durable re-point files/functions are all listed (coverage of the known reader set).

**Rationale**: Splits the "must survive a swap" concern (generated-product scan, presence-only) from the "authored content is accurate" concern (template gate, exact-symbol). Matches the existing split between `GovernanceTests.fs` (durable, in the product) and `tests/Package.Tests/FeatureNNN*Tests.fs` (template invariants, in the framework repo).

## Decision 5 — Internal test naming (avoid the FeatureNNN collision)

**Decision**: Name the new template-authoring test **descriptively** (`SwapChecklistTemplateTests` / `BuildHelpBannerTemplateTests`), not `Feature242*`. Reference "spec 242-scaffold-discoverability" in a header comment.

**Rationale**: Internal `FeatureNNN` test identifiers diverge from `specs/NNN` numbers, and **`tests/Build.Tests/Feature242DocsCurrencyTests.fs` already exists** (an earlier docs-currency item that happened to take internal number 242). Reusing `Feature242` would collide/confuse. Max internal number in use is 242; descriptive names sidestep the numbering entirely and are unambiguous.

## Decision 6 — Additivity / contract safety (FR-010)

**Decision**: No change to any versioned cross-repo contract (`scaffold-provider`, `scaffold-provenance`, `fs-gg-ui-template`), the six-scanned-file order, or the must-survive evidence-token set. The additions are: three new authored fragment files, three new `sources[]` entries, a `build.fsx`/`build.sh` help branch, and new test assertions. The registry does not move; no `contract-change` issue is required.

**Rationale**: Confirmed the base source is unconditional and the six scanned scaffold files + evidence tokens are untouched. Adding a root doc and a help branch changes neither the template's public parameters nor the package surface. Scaffold-map already documents the durable/replaceable split the checklist projects, so the two stay coherent.

## Open items for `/speckit-tasks`

- Exact per-profile symbol lists to render in each `SWAP-CHECKLIST.md` (enumerated in `data-model.md`).
- Whether the app-family and governed-family checklists are worth their own file or a lighter shared stub — default: three files (one per model family) for precision; revisit only if a family's re-point set is trivially small.
- SC-002 honesty caveat: full "zero omissions" cannot be mechanically proven without F# parsing; the template gate proves *no phantoms* + *all known reader functions listed*, and the author + game golden cover omissions. Record this limitation in the test header (constitution Principle V — honest evidence).
