# US2 — Uniform verb wrapper delegating to FAKE (Feature 212)

Quickstart Scenario B verified on the scaffolded `Acme` product 2026-06-28. Every verb routes
through the single FAKE entry `dotnet fsi build.fsx -t <Target>` (the wrapper maps verb→target and
exec's that one command — see `template/base/build.sh` / `build.cmd`).

## Verb behaviour (POSIX `./build.sh`)

| Invocation | Result | Exit |
|---|---|---|
| `./build.sh restore` | `dotnet fsi build.fsx -t Restore` → stock restore over `Acme.slnx` | 0 |
| `./build.sh build` | `-t Build` → `Acme -> Acme.dll`, `Acme.Tests -> Acme.Tests.dll`, Build succeeded | 0 |
| `./build.sh test` | `-t Test` → `Passed! Failed: 0, Passed: 30` (`Test completed`) | 0 |
| `./build.sh run` | `-t Run` → `status=ok ... exit-path=true` (app profile) | 0 |
| `./build.sh verify` | `-t Verify` → frozen rich path: Dev/GuidanceCheck/TemplateDrift + EvidenceGraph + EvidenceAudit + 30 tests, `Verify completed` | 0 |
| `./build.sh pack` | `-t Pack` → `Successfully created package '.../Acme.1.0.0.nupkg'` (NU5104 prerelease-dep warnings only) | 0 |
| `./build.sh bogus` | `build.sh: unknown verb 'bogus'` + supported-verb list | **2** |
| `./build.sh` (missing) | `build.sh: missing verb` + supported-verb list | **2** |

## SC-004 — `verify`/`test` semantics unchanged

- `./build.sh test` ≡ FAKE `Test` and `./build.sh verify` ≡ FAKE `Verify` **by construction**: the
  wrapper's only action for those verbs is `dotnet fsi build.fsx -t Test|Verify` — the same single
  FAKE entry. No alternate `test`/`verify` implementation exists to diverge from.
- The frozen `Test`/`Verify` bodies in `build.fsx` are byte-unchanged by this feature (T008); the
  live `verify` run above exercised that frozen path end-to-end (exit 0).

## SC-003 — Both shells expose equivalent verbs

Static parity (the Windows `build.cmd` cannot run on this Linux box, so parity is confirmed by
inspection):

```
build.sh verbs:     restore build test run verify pack
build.cmd verbs:    restore build test run verify pack
build.cmd targets:  target=Restore target=Build target=Test target=Run target=Verify target=Pack
```

Both wrappers: same six verbs → same FAKE targets; unknown/missing verb → supported-verb list +
non-zero exit (`exit /b 2` in `build.cmd`, `exit 2` in `build.sh`).

**Checkpoint**: Verb wrapper works on POSIX and delegates to FAKE; `verify`/`test` semantics frozen;
unknown/missing verbs reported. US1 + US2 both independently functional.
</content>
