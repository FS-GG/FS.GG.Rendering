# T003 / T008 — Emission & root-cause map (Feature 212)

## Base source (the ungated product content root)

`.template.config/template.json` `sources[0]`:

```jsonc
{ "source": "template/base/", "target": "./",
  "exclude":  ["bin/**","obj/**","**/bin/**","**/obj/**",".agents/**",".claude/**","CLAUDE.md"],
  "copyOnly": ["docs/api-surface/**","docs/evidence-formats.md","docs/skillist-reference.md",
               "docs/scaffold-map.md","docs/interactive-readiness.md"] }
```

The base source copies **every** non-excluded file under `template/base/` to the product root with
default `sourceName = "Product"` substitution. The four new top-level files are therefore emitted
automatically — **no `template.json` edit is required** (T006: "add nothing to `exclude`; do not
`copyOnly`-freeze `Product.slnx`"). The desired wiring is the *absence* of any new exclude/copyOnly
entry.

| New file | Excluded? | copyOnly? | sourceName rewrite | Result |
|---|---|---|---|---|
| `template/base/Product.slnx` | no | **no** (needs the rewrite) | filename `Product`→`<Name>`; content `src/Product/Product.fsproj`→`src/<Name>/<Name>.fsproj`, `tests/Product.Tests/Product.Tests.fsproj`→`tests/<Name>.Tests/<Name>.Tests.fsproj` | `<Name>.slnx` emitted |
| `template/base/global.json` | no | no | content-neutral (no `Product`/`product` token) | `global.json` emitted verbatim |
| `template/base/build.sh` | no | no | content-neutral (name-agnostic; locates the single `.slnx`/`src` project at runtime) | `build.sh` emitted |
| `template/base/build.cmd` | no | no | content-neutral | `build.cmd` emitted |

These live in the ungated base source (not the `lifecycle == "spec-kit"`-gated `.agents/`/`.claude/`
sub-sources, nor the `designSystem == "ant"` overlay), so they ship for **every** `lifecycle`
(`spec-kit`/`sdd`/`none`) and are **byte-neutral** across `designSystem` (`wcag`/`ant`) — FR-008.
Precedent: the pre-existing `fake.sh`/`fake.cmd` in the same directory emit by the same rule.

## Verb → FAKE target table (contract C3)

| Verb | FAKE target | New / frozen | Action in `build.fsx` |
|---|---|---|---|
| `restore` | `Restore` | **new** | `dotnet restore "<single .slnx>"` |
| `build` | `Build` | **new** | `dotnet build "<single .slnx>"` |
| `test` | `Test` | **frozen** | unchanged: `dotnet test tests/Product.Tests/... -m:1 --disable-build-servers` |
| `run` | `Run` | **new** | `dotnet run --project src/<single src project>` |
| `verify` | `Verify` | **frozen** | unchanged rich evidence+test path |
| `pack` | `Pack` | **new** | `dotnet pack "<single .slnx>" -c Release` |

The new targets locate the single `*.slnx` and single `src/<project>` name-agnostically
(`singleRootSolution`/`singleSrcProject`), so no literal `<Name>` is baked into the script.

## T008 — Frozen seams (FR-007 / SC-004)

- `Test` and `Verify` bodies in `template/base/build.fsx` are **byte-for-byte unchanged** by this
  feature. The only edits are (a) two new private locator functions and (b) four new `match` arms
  (`Restore`/`Build`/`Run`/`Pack`) added *above* the `Test`/`Verify` arms. `runGeneratedTests`,
  `Test`, and the `Verify` block (Dev/GeneratedGuidanceCheck/TemplateDrift + EvidenceGraph +
  EvidenceAudit + tests) are untouched.
- **FR-010 parity intent**: the stock root path (`dotnet build <Name>.slnx`) and the FAKE `Build`
  target both build the *same* root `.slnx` (FAKE `Build` shells to `dotnet build "<single .slnx>"`),
  so the project sets cannot diverge by construction. **Intent only here; actual parity is verified
  live in T011.**
</content>
