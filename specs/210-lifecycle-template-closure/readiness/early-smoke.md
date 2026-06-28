# Early Live Smoke Run — Feature 210 (T005)

**Standing obligation (plan.md):** prove the *published package* behaves like the working tree the
children validated, BEFORE building the harness/record — so the close/don't-close work rests on
observed behavior, not inference. Per research.md R1.

## What was run

```bash
dotnet new install FS.GG.UI.Template::0.1.51-preview.1 --add-source ~/.local/share/nuget-local/
dotnet new list fs-gg-ui          # → resolves to the PACKAGE (FS.GG.UI.Template 0.1.51-preview.1)
dotnet new uninstall              # → confirmed installed source is the package, not the working tree
# spot-check matrix:
dotnet new fs-gg-ui --name Acme --profile app            --lifecycle spec-kit -o <tmp>
dotnet new fs-gg-ui --name Acme --profile app            --lifecycle sdd      -o <tmp>
dotnet new fs-gg-ui --name Acme --profile headless-scene --lifecycle none     -o <tmp>
dotnet new uninstall FS.GG.UI.Template   # restore dev environment
```

Environment: dotnet SDK `10.0.301`, `dotnet new` engine available. `--name Acme` used (PascalCase)
to avoid the dir-derived-lowercase-name build break noted in prior features.

## Observed (spot-check)

| lifecycle / profile | `.specify/` | `.claude/` | `.agents/` | `AGENTS.md`/`CLAUDE.md` | product |
|---|---|---|---|---|---|
| `spec-kit` / `app` | PRESENT | PRESENT | PRESENT | both ✓ | present |
| `sdd` / `app` | absent | absent | absent | both ✗ | present |
| `none` / `headless-scene` | absent | absent | absent | both ✗ | present |

- The installed template resolves to **`FS.GG.UI.Template 0.1.51-preview.1`** (the package a consumer
  pulls), confirmed via `dotnet new uninstall` listing — not the working-tree `.template.config/` source.
- The gated lifecycle set is **present only under `spec-kit`** and **fully suppressed under `sdd`/`none`**;
  the generated product is present in every cell.
- Directive `AGENTS.md`/`CLAUDE.md` agent-context files appear only under `spec-kit`. (A broad text scan
  of the `none` tree surfaces `.specify/`/`.agents/` strings only inside the *copyOnly governance
  reference docs* — `docs/evidence-formats.md`, `docs/skillist-reference.md` — which document the skill
  convention and are out of the gated set by design, per Feature 204. The harness applies 204's precise
  directive-doc check, which scans `CLAUDE.md`/`AGENTS.md`/`README.md` only.)

## Hypothesis resolution

**Hypothesis** (plan.md / spec edge case "drift between child evidence and the published artifact"):
*the published package behaves like the working tree the children validated.*

**Resolution: CONFIRMED.** The installed `0.1.51-preview.1` package emits the full gated lifecycle
surface under `spec-kit` and suppresses it under `sdd`/`none` while keeping the product intact —
matching the 204/206 working-tree evidence. The acceptance harness (US1) proceeds to formalize this
across the full 3×4 matrix with byte-identity + build spot-check and write the consolidated record.
