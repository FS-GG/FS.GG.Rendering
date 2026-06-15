# Quickstart — Validating Feature 122 (Harness Input Backends)

Net-new, contract-first: this guide validates the work **once built** (`Input.fsi`/`Input.fs` + the CLI wiring +
tests). The `pure` arm is the gate-runnable MVP; `x11-xtest`/`uinput` are env-gated (honest-skip headless).

## Prerequisites

- .NET `net10.0` SDK; repo restored. `pure` needs **no** display/GL. `x11-xtest` needs a display/GL host
  (Xvfb + EGL); `uinput` needs `/dev/uinput` (+ a running `ydotoold`).

## 1. Build (Release, zero warnings)

```bash
dotnet build FS.GG.Rendering.slnx -c Release   # expect 0 warnings, 0 errors
```

## 2. Run the pure backend headlessly (the MVP — US1/US2)

```bash
dotnet run --project tests/Rendering.Harness -c Release -- input --backend pure --script <name> --out /tmp/in --json
```

Expected: **exit 0**; `/tmp/in/run.json` with `status` = passed and a **non-empty** `notAuthoritativeFor`.
Re-run the same command and diff the two `run.json` — they MUST be **byte-identical** (deterministic; injected
`Wait`, no wall-clock) (SC-001/SC-002).

## 3. Env-gated arms degrade-and-disclose (US3/US4)

```bash
dotnet run --project tests/Rendering.Harness -c Release -- input --backend uinput    --script <name> --out /tmp/in   # /dev/uinput absent ⇒ exit 0, status=skipped, disclosed reason, prompt (SC-003)
dotnet run --project tests/Rendering.Harness -c Release -- input --backend x11-xtest --script <name> --out /tmp/in   # headless ⇒ exit 0, status=skipped, disclosed reason (SC-004); on a display/GL host ⇒ before/after repaint, status=passed
```

Neither hangs; neither fakes a pass. Unknown `--backend`/`--script` ⇒ non-zero exit with a clear message
(FR-003).

## 4. Harness unit tests (the gate proof — A6)

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release
```

Expected green: the planner decision per `InputBackend`; `NotAuthoritativeFor` never empty (SC-005); the
clean-skip status/exit for `uinput`/`x11-xtest` when their capability is absent; and a **deterministic
`pure`-replay golden** (byte-identical evidence over two runs, SC-002).

## 5. Confirm zero public-surface change (FR-009/SC-006)

```bash
git status -s tests/surface-baselines/   # MUST be empty — harness is not part of the public package surface
```

## 6. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing skips unrelated to 122
```

## What this does NOT prove (headless)

- The `x11-xtest` real-input→repaint and `uinput` kernel-input proofs run only on a capable runner; headless
  they honest-skip (capable-runner provisioning is Workstream B). Every run discloses this via a non-empty
  `notAuthoritativeFor`.

## Success = the Workstream-A near-term bar

Build green; the `pure` backend runs in the gate (deterministic, non-empty `notAuthoritativeFor`); the
env-gated arms honest-skip; harness unit tests green; zero public-surface change; `/speckit-analyze` consistent.
</content>
