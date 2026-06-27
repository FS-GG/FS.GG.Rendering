# Contract: Engine invocation (generated `build.fsx` ↔ `FS.GG.UI.Build` engine)

This is the reflected boundary the generated `build.fsx` already calls. The re-authored engine MUST
satisfy it **exactly** — `build.fsx` resolves the engine by simple-name reflection and
`template/base/tests/Product.Tests/GovernanceTests.fs` scans the script text for these symbols.

## Resolution contract (consumer side — `build.fsx`, already implemented)

- Reads `<FsSkiaUiVersion>` from `Directory.Packages.props` (single source of version truth).
- Locates the engine assembly in the NuGet global-packages cache at
  `<packages>/fs.gg.ui.build/<version>/lib/net10.0/FS.GG.UI.Build.dll`.
  - **FIX (FR-002):** the current literal is `fs.skia.ui.build` (`build.fsx:~126`) — correct to
    `fs.gg.ui.build`. NuGet lowercases package-id folders.
- If absent, restores the pinned `FS.GG.UI.Build` version via a throwaway project (default NuGet
  config ⇒ local feed in-repo, nuget.org for a published consumer).
- If still absent: **fail loudly** with a message naming the engine identity (`FS.GG.UI.Build
  <version>`) and the cache path/feed searched (FR-005). Never proceed, never report success.
- Loads the engine via `Assembly.LoadFrom`; resolves the engine's transitive closure on demand from the
  same global cache through an `AppDomain.AssemblyResolve` handler.

## Reflected entrypoint (engine side — MUST provide)

| Aspect | Value |
|--------|-------|
| Namespace + type | `FS.GG.UI.Build.Evidence.GeneratedRunner` |
| Member | static `run` |
| Signature (F#) | `run : target:string -> dir:string -> int` |
| Reflection call | `runnerType.GetMethod("run").Invoke(null, [| box target; box dir |]) :?> int` |
| `target` values | `"EvidenceGraph"` and `"EvidenceAudit"` (exact strings) |
| `dir` | the generated product working directory (`Directory.GetCurrentDirectory()`) |
| Return | exit code; `0` = pass; non-`0` = fail (build.fsx fails the target on non-0) |

Notes:
- The method is invoked as a **static** member with two boxed string args; F# `let`-bound module
  functions surface as static methods on the module type, which is the shape the reflection expects.
  The `.fsi` MUST keep `run` public on `GeneratedRunner` (a module or a type with a static member —
  matching `assembly.GetType("…GeneratedRunner")` + `GetMethod("run")`).
- No typed `open` of the engine exists in `build.fsx` (so no version pin leaks) — the contract is
  reflection-only by design (Feature 064 R1).

## Consumer-text invariants (GovernanceTests — MUST stay green, FR-007)

The generated `build.fsx` text MUST continue to contain:
- `runGeneratedEvidence "EvidenceGraph"` and `runGeneratedEvidence "EvidenceAudit"`
- `GeneratedRunner`
- `Assembly.LoadFrom`
- `FsSkiaUiVersion`
- **no** match for `#r "nuget: FS.Skia.UI.Build,"` (regex-asserted)
- **not** the completion-only-log pattern `| "EvidenceGraph"\n    | "EvidenceAudit" -> writeLog target`
- **no** decommissioned scripts: `run-audit.sh`, `compute-task-graph.py`, `python3`,
  `ProcessStartInfo("bash"`, `chmod`
- clean-text-log markers: `RedirectStandardOutput <- true`, `RedirectStandardError <- true`,
  `let output = stdout + stderr`, `tryWriteTextLog logPath output`, `printf "%s" output`;
  **no** `File.WriteAllBytes` / `BinaryWriter` / NUL / `Array.zeroCreate`.

## Optional strengthening (satisfies US2 acceptance #3, FR-002)

Add a governance scan asserting the generated `build.fsx` contains **no** pre-rebrand identifier —
`fs.skia.ui.build` (cache path) or `FS.Skia.UI` (package name) — so the corrected path cannot silently
regress. Must be added without breaking existing assertions.
