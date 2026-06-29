# Quickstart: validate the `productName` scaffold symbol

Runnable validation that proves the feature end-to-end. Implementation details live in `tasks.md` / `data-model.md`; this is the run/validation guide.

## Prerequisites

- .NET SDK with `net10.0` (repo standard).
- For the live tier: `dotnet` CLI on PATH. For full cross-repo proof (SC-001/SC-005): `fsgg-sdd` ≥ 0.2.0 and `FS.GG.UI.Template` on the org NuGet feed.
- Contract under test: [`contracts/productname-scaffold-provider.md`](./contracts/productname-scaffold-provider.md). Symbol design: [`data-model.md`](./data-model.md).

## 1. Always-on verdict-core gate (deterministic, no `dotnet new`)

Re-derives the contract fact straight from `template.json` (productName symbol present + additive; `effectiveName` coalesce drives the rename; `projectSlug` sources `effectiveName`):

```
dotnet test tests/Package.Tests -c Release --filter Feature217
```

Expected: green on a fresh checkout (the gate self-provisions its report from the validator's `--emit-report` verdict core, then asserts it). Fails loudly if the `productName` wiring is missing or the contract regresses.

## 2. Live instantiation matrix + byte-diff (env-gated, real `dotnet new`)

```
FS_GG_RUN_PRODUCTNAME_VALIDATION=1 dotnet fsi scripts/validate-productname-template.fsx
```

This validator (models `scripts/validate-lifecycle-template.fsx`) MUST:

| Check | Asserts | Spec |
|---|---|---|
| `--productName Acme` (no `-n`) instantiates | no exit 127; tree named `Acme` | G1/G2, SC-001 |
| `--productName Acme` ≡ `-n Acme` (byte-diff) | zero diff | G2/G3, SC-004 |
| no-`productName` matrix ≡ pre-change baseline (byte-diff) | zero diff | G5, SC-003 |
| empty/whitespace `--productName ""` | falls back to default | G4, FR-006 |
| `dotnet build -c Release` of the `Acme` product | 0 warnings / 0 errors | G6, SC-002 |

It writes the committed-style report to `specs/217-template-productname-symbol/readiness/productname-template-validation.md` (gitignored, self-provisioned), which the gate in step 1 asserts.

> Capture the **pre-change baseline** before editing `template.json` (clean worktree / `HEAD` template, or the published `FS.GG.UI.Template@<current>`). See `research.md` R2.

## 3. End-to-end SDD composition (cross-repo, when feed + toolchain available)

```
fsgg-sdd scaffold --provider rendering --param productName=Acme
dotnet build Acme.slnx -c Release
```

Expected: scaffold succeeds (was exit 127), product is named `Acme`, build is clean (SC-001/SC-002). Equivalent low-level form:

```
dotnet new fs-gg-ui -o Acme --productName Acme --profile app --lifecycle sdd --designSystem wcag
dotnet build Acme/Acme.slnx -c Release
```

## 4. Cross-repo contract record (SC-005)

Record the additive `scaffold-provider` change in the org registry (`FS-GG/.github`) and cross-reference #27 ⇄ SDD#35 via the cross-repo-coordination protocol. Verify: registry shows the additive change; the two issues agree the Rendering side honors `productName`.

## Done when

- Step 1 gate green; step 2 validator reports all checks pass (or env-skipped tiers explicitly disclosed); step 3 composition succeeds where it previously failed with exit 127; step 4 registry coherent.
