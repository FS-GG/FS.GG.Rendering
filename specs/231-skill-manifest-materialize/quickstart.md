# Quickstart — validating Feature 231

Prerequisites: .NET 10 SDK; repo root; no network needed except NuGet restore (nuget.org).

## 1. Env-free gates (the release gate set)

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release
dotnet fsi scripts/validate-lifecycle-template.fsx            # verdict core self-check
```

Expected: Feature231 manifest/parity/no-dangling/target-shape tests green; reworked
Feature 204/219 gates green against the new emission table (data-model.md §source-row delta).

## 2. Live scaffold proof (env-gated)

```sh
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx
```

Expected report lines (`specs/204-.../readiness/lifecycle-template-validation.md`,
`provenance: live`): per profile `spec-kit/<p>: three-root-mirror=ok (materialized)`,
`manifest-digests=ok`, `dangling-routes=0`; `sdd|none/<p>: claude-product-skills=0
codex-product-skills=0`.

## 3. Manual end-to-end (one profile)

```sh
dotnet new install .   # or the packed nupkg
dotnet new fs-gg-ui -n Zebra -o /tmp/zebra --profile app
ls /tmp/zebra/.codex 2>/dev/null                     # absent at generation
cd /tmp/zebra && ./build.sh build                    # first build materializes
diff -r .agents/skills .claude/skills && diff -r .agents/skills .codex/skills
grep -rL "zebra" .agents/skills/*/SKILL.md           # skill prose not name-rewritten
cat .agents/skills/skill-manifest.json               # catalog, digests
dotnet fsi .specify/scripts/fs-gg/materialize-skill-roots.fsx --enforce   # idempotent, ok
```

Expected: three identical roots; no `fs-gg-product-*`/wrapper dirs; skill bodies keep the
English word "product"; `--enforce` exits 0.

## 4. Parity spot-check

`dotnet test tests/Package.Tests -c Release --filter "Feature231"` — includes the
perturbation case (vendored behavior change ⇒ red).
