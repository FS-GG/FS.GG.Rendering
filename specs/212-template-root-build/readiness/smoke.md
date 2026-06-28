# T007 — Early live smoke (Feature 212)

**Live instantiate-and-run smoke against a really-scaffolded product.** Confirms the plan's two
build hypotheses BEFORE the verb wrapper / release wiring is hardened. Run 2026-06-28 on branch
`212-template-root-build`.

## Steps & evidence

```
$ dotnet new install .            # in-repo source template wins over packed 0.1.52-preview.1
Success: ... FS GG UI Governed Project / fs-gg-ui

$ dotnet new fs-gg-ui --name Acme --output "$(mktemp -d)/Acme"
The template "FS GG UI Governed Project" was created successfully.   # EXIT 0
```

Root artifacts emitted with correct `sourceName` rewrite (PascalCase `--name Acme` per memory:
fs-gg-ui template needs PascalCase name):

```
Acme.slnx  global.json  build.sh  build.cmd      # all four present at product root

# Acme.slnx (Product → Acme rewrite of filename, project paths AND references):
<Solution>
  <Project Path="src/Acme/Acme.fsproj" />
  <Project Path="tests/Acme.Tests/Acme.Tests.fsproj" />
</Solution>

# global.json (verbatim, no placeholder token):
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature", "allowPrerelease": false } }

# src/Acme  tests/Acme.Tests  (directories renamed by sourceName)
```

Stock toolchain at the **product root** (no FAKE, no knowledge of build.fsx):

```
$ dotnet restore     # EXIT 0 — Restored src/Acme/Acme.fsproj + tests/Acme.Tests/Acme.Tests.fsproj
$ dotnet build       # EXIT 0 — Acme -> .../Acme.dll ; Acme.Tests -> .../Acme.Tests.dll
                     #          Build succeeded. 0 Warning(s) 0 Error(s)
$ dotnet test        # EXIT 0 — Passed! Failed: 0, Passed: 30, Skipped: 0, Total: 30
$ dotnet run --project src/Acme   # EXIT 0
```

## Hypotheses

- **(a) A single root `.slnx` makes stock `dotnet build/test/run` resolve — CONFIRMED.**
  `dotnet restore`/`build`/`test` with no project argument all resolved `Acme.slnx` and succeeded.

- **(b) Headless `dotnet run` exits 0 via `UnsupportedEnvironment` — CONFIRMED (exit 0).**
  **Caveat / nuance:** this box has a live Wayland session
  (`display-variable=WAYLAND_DISPLAY=wayland-0`), so on the FIRST scaffold (`Acme`) `dotnet run`
  took the **interactive-window** path. The app profile opens a *persistent* window that blocks
  until closed — a second scaffold (`GeneratedProduct`, T018) confirmed this by hanging until the
  `timeout` fired (the earlier `Acme` exit-0 was a window-close fluke, not the deterministic path).
  The release gate runs on a **genuinely headless** ubuntu runner with no display. Reproducing that
  locally (`env -u WAYLAND_DISPLAY -u DISPLAY -u XDG_RUNTIME_DIR ... dotnet run`) yields the
  deterministic safe-degrade: `status=unsupported classification=UnsupportedEnvironment
  unsupported-host-reasons=XDG_RUNTIME_DIR` → **exit 0** (no hang). This is exactly the headless CI
  path (research R4), so the gate's plain `dotnet run` assertion is sound and **the
  `tryRunEvidenceCommand` fallback is NOT needed** — T017 uses a plain `dotnet run`. (On a real
  desktop the same command opens the persistent window — out of scope for CI.)

## Conclusion

Both plan hypotheses hold against a real scaffold. No update to plan.md/research.md required. The
Foundational checkpoint is met; US1/US2/US3 may proceed.
</content>
