# Quickstart / Validation: Root-buildable generated products

Proves Feature 212 end-to-end: scaffold a product from the template under development and confirm it is
root-buildable with the **stock** .NET CLI, that the **verb wrapper** delegates to FAKE, and that the
**release gate** catches regressions. Details live in [contracts](./contracts/template-root-build.contract.md)
and [data-model.md](./data-model.md) — this is the run guide.

## Prerequisites

- .NET 10 SDK (`dotnet --version` → 10.0.x). This box also has 6.0.428 installed — exactly the
  default-SDK-mismatch case `global.json` must neutralize.
- Repo: `FS.GG.Rendering`, branch `212-template-root-build`.

## Scenario A — Stock root build/test/run (US1, the core)

```sh
# Install the in-repo template and scaffold a product into a temp dir
dotnet new install .
work="$(mktemp -d)"; prod="$work/Acme"
dotnet new fs-gg-ui --name Acme --output "$prod"

# Confirm the root artifacts exist and carry the real name
ls "$prod"/Acme.slnx "$prod"/global.json "$prod"/build.sh "$prod"/build.cmd

# Stock toolchain at the product ROOT (no FAKE)
cd "$prod"
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Acme        # headless: prints status=..., exits 0
```

**Expected**: restore/build/test succeed; `dotnet run` exits 0 (headless `UnsupportedEnvironment`
degrade). Use `--name Acme` (PascalCase) — a lowercase/dir-derived name breaks the generated build
(memory: fs-gg-ui template needs PascalCase name).

## Scenario B — Uniform verb wrapper delegates to FAKE (US2)

```sh
cd "$prod"
./build.sh restore
./build.sh build
./build.sh test       # ≡ FAKE Test (unchanged)
./build.sh run        # app profile
./build.sh verify     # ≡ FAKE Verify (unchanged rich path)
./build.sh pack
./build.sh bogus      # prints supported verbs, non-zero exit
```

**Expected**: each verb routes through `dotnet fsi build.fsx -t <Target>`; `verify`/`test` behave exactly
as before this feature; unknown verb is reported, not silently ignored.

## Scenario C — SDK pin reproducibility (SC-006)

```sh
cd "$prod"
cat global.json                      # 10.0.x band, rollForward latestFeature
dotnet build                         # resolves the pinned net10 SDK even though default may be 6.0.x
```

**Expected**: build uses the net10 SDK band regardless of machine default; a host lacking the band fails
fast with an SDK-resolution error (not a silent wrong-SDK build).

## Scenario D — Release regression gate (US3 / SC-005)

```sh
# Locally mirror the release job's assertions (what release.yml runs):
dotnet new install .
work="$(mktemp -d)"; prod="$work/GeneratedProduct"
dotnet new fs-gg-ui --name GeneratedProduct --output "$prod"
dotnet build "$prod"
dotnet test  "$prod"
dotnet run --project "$prod/src/GeneratedProduct"   # exit 0

# Negative check: break root buildability (e.g. remove the slnx) → assertions must fail
rm "$prod"/GeneratedProduct.slnx
dotnet build "$prod"      # EXPECTED: fails (no root solution) → gate would block release
```

**Expected**: with the artifacts present all three assertions pass; removing the root solution makes
`dotnet build "$prod"` fail — demonstrating the gate blocks regressions.

## Scenario E — Profile / lifecycle / designSystem coverage (FR-008)

```sh
for p in app headless-scene governed sample-pack; do
  for lc in spec-kit sdd none; do
    d="$(mktemp -d)/P"; dotnet new fs-gg-ui --name P --profile "$p" --lifecycle "$lc" --output "$d"
    ls "$d"/P.slnx "$d"/global.json "$d"/build.sh "$d"/build.cmd   # all present
    dotnet build "$d" && dotnet test "$d"                          # build/test all profiles
    # `run` asserted only for runnable (app) profile
  done
done
```

**Expected**: root artifacts emitted and build/test succeed for every profile × lifecycle; `wcag` vs
`ant` `designSystem` makes no difference to these artifacts.

## Cleanup

```sh
dotnet new uninstall .   # remove the locally installed template
```

## Done when

- Scenarios A–E pass on a really-scaffolded product (not just unit tests — this is the live
  instantiate-and-run smoke the plan's Foundational phase mandates).
- The extended `release.yml` job is green on a passing product and red when root buildability is broken.
