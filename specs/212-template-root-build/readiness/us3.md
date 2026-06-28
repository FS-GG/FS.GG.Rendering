# US3 — Release gate proves root buildability (Feature 212)

Quickstart Scenario D verified locally 2026-06-28, mirroring the extended
`.github/workflows/release.yml` → `template-product-tests` job exactly (`--name GeneratedProduct`).

## T017 — Gate wiring (contract C4)

The existing job's instantiate step is preserved (sets `PRODUCT_DIR`); the prior single
`dotnet test "$PRODUCT_DIR" -c Release` step is **extended, not duplicated** into one step asserting
all three stock commands:

```yaml
- name: Build, test, and run generated product (stock root, Feature 212)
  run: |
    set -euo pipefail
    dotnet build "$PRODUCT_DIR"
    dotnet test "$PRODUCT_DIR"
    dotnet run --project "$PRODUCT_DIR/src/GeneratedProduct"
```

- **Chosen config: stock default (Debug) for all three** — matches `quickstart.md` and the SDD
  composition-acceptance probe surface (stock declared-or-default commands). The US1 smoke confirmed
  the stock default path builds/tests/runs; Release is NOT required, so the existing `-c Release` on
  the test line was dropped to keep the gate proving exactly what a consumer runs.
- The job-level guard `if: github.repository == 'FS-GG/FS.GG.Rendering'` is preserved (unchanged).
- `dotnet run` exits 0 on the headless runner via the entrypoint's `UnsupportedEnvironment`
  safe-degrade (research R4 — confirmed in smoke.md and below); `set -e` blocks the job on any
  non-zero exit.

## T018 — Two-way demonstration (SC-005)

**Passing run** (artifacts present), mirroring the three job assertions:

```
$ dotnet build "$PRODUCT_DIR"                              # Build succeeded.  EXIT 0
$ dotnet test  "$PRODUCT_DIR"                              # Passed! Failed: 0, Passed: 30  EXIT 0
$ dotnet run --project "$PRODUCT_DIR/src/GeneratedProduct" # headless (display vars cleared):
    status=unsupported classification=UnsupportedEnvironment unsupported-host-reasons=XDG_RUNTIME_DIR
                                                            # EXIT 0
```

(Note: on this box's live Wayland session the run opens a persistent window and blocks; the headless
exit-0 above was produced with `env -u WAYLAND_DISPLAY -u DISPLAY -u XDG_RUNTIME_DIR`, reproducing
the ubuntu CI runner. See smoke.md hypothesis (b).)

**Deliberately-broken run** — remove the root solution, stock build must fail (gate blocks release):

```
$ rm "$PRODUCT_DIR/GeneratedProduct.slnx"
$ dotnet build "$PRODUCT_DIR"
MSBUILD : error MSB1003: Specify a project or solution file. The current working directory does not
contain a project or solution file.                       # EXIT 1
```

With `set -e`, that non-zero exit fails the step → fails the job → blocks the release. SC-005 holds
both ways: green when root buildability is present, red when it is broken.

**Checkpoint**: Release gate is green on a passing product and red when root buildability is broken —
all three stories functional.
</content>
