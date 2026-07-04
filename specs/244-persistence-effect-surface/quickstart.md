# Quickstart / Validation: Persistence effect surface + fs-gg-persistence skill

Feature `244-persistence-effect-surface`. Runnable checks that prove the feature works end-to-end.
Details live in `contracts/Persistence.fsi`, `data-model.md`, and `research.md`; this is the run
guide.

## Prerequisites

- .NET SDK (repo standard, `net10.0`); no filesystem/writable save location required (headless-safe
  by design).
- Local pack feed at `~/.local/share/nuget-local/` for the packed-library / template checks.

## 1. Pure request surface + record-only interpreter (US1 + US2)

Build Canvas and exercise the persistence surface through FSI, exactly as a consumer would
(Constitution Principle I — FSI is the honest audience):

```sh
dotnet build src/Canvas/Canvas.Lib.fsproj
dotnet fsi   # then #r the built FS.GG.UI.Canvas.dll and:
#   open FS.GG.UI.Canvas
#   let ev = Persistence.interpret [ Persistence.save (Persistence.saveEnvelope 1 (SaveSlot "slot-1") "{score:42}")
#                                    Persistence.load (SaveSlot "slot-1")
#                                    Persistence.deleteSlot (SaveSlot "old") ]
#   ev.Requested
#   // -> [ Save { Version=1; Slot=SaveSlot "slot-1"; Payload=SavePayload "{score:42}" }
#   //      Load (SaveSlot "slot-1"); DeleteSlot (SaveSlot "old") ]
```

**Expected**: requests recorded in dispatch order; version normalized (a negative version clamps to
`minVersion`); opaque payload carried verbatim; unknown-slot load/delete recorded, no exception; no
IO/filesystem access. Automated equivalent:

```sh
dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj
```

**Expected**: new semantic tests fail before the surface exists and pass after — they assert the
exact `PersistenceEvidence.Requested` sequence (slot, version, payload) for a set of game events
(checkpoint save, continue-game load, erase-save delete) driven through a pure model, with zero IO in
`update` (SC-001, SC-002).

## 2. Skill materialization gating (US3)

Instantiate the template for a persistence profile and a non-persistence profile, and diff the skill
roots:

```sh
# game profile -> fs-gg-persistence present
dotnet new fs-gg-ui -o /tmp/244-game  --profile game
ls /tmp/244-game/.agents/skills/fs-gg-persistence/SKILL.md          # expected: present

# app profile -> fs-gg-persistence absent
dotnet new fs-gg-ui -o /tmp/244-app   --profile app
ls /tmp/244-app/.agents/skills/fs-gg-persistence 2>/dev/null || echo "absent (expected)"
```

**Expected**: exactly one `fs-gg-persistence` skill for `game`/`sample-pack`, zero for
`app`/`headless-scene`/`governed` (SC-003, SC-005). Non-persistence profiles are byte-unchanged
w.r.t. persistence.

## 3. Manifest / template / parity coherence

```sh
dotnet fsi scripts/generate-skill-manifest.fsx        # regenerates fs-gg-persistence sha256; must be a no-op diff after commit
dotnet test tests/Package.Tests/Package.Tests.fsproj  # Feature231SkillManifestTests + SurfaceAreaTests
```

**Expected**:
- The manifest `fs-gg-persistence` entry's `sha256` matches the committed SKILL.md body;
  regenerating is a no-op (SC-003, SC-004).
- `materializes-when` (manifest) and the `condition` (template.json) carry the **same**
  `profile in [game, sample-pack]` predicate; the SkillParity harness passes (wrappers present in
  both `.agents/` and `.claude/`, canonical body referenced, no framework-process vocabulary leak).

## 4. Surface baseline (Principle II guard)

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx     # must be a no-op diff after the baseline is committed
dotnet test tests/Package.Tests/Package.Tests.fsproj # SurfaceAreaTests
```

**Expected**: `readiness/surface-baselines/FS.GG.UI.Canvas.txt` includes the new `Persistence`
module/types and the surface-drift gate is green.

## 5. Skill currency / de-leak

**Expected**: every persistence API the skill references resolves to shipped surface (no dangling
references), and the skill passes the existing skill-currency / de-leak checks — consumer vocabulary
only, no framework-process terms (SC-004).
