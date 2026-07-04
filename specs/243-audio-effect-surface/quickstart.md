# Quickstart / Validation: Audio effect surface + fs-gg-audio skill

Feature `243-audio-effect-surface`. Runnable checks that prove the feature works end-to-end.
Details live in `contracts/Audio.fsi`, `data-model.md`, and `research.md`; this is the run guide.

## Prerequisites

- .NET SDK (repo standard, `net10.0`); no audio hardware required (headless-safe by design).
- Local pack feed at `~/.local/share/nuget-local/` for the packed-library / template checks.

## 1. Pure request surface + record-only interpreter (US1 + US2)

Build Canvas and exercise the audio surface through FSI, exactly as a consumer would
(Constitution Principle I — FSI is the honest audience):

```sh
dotnet build src/Canvas/Canvas.Lib.fsproj
dotnet fsi   # then #r the built FS.GG.UI.Canvas.dll and:
#   open FS.GG.UI.Canvas
#   let ev = Audio.interpret [ Audio.playSfx (SoundId "fire") 0.8
#                              Audio.playMusic (TrackId "level1") true
#                              Audio.setMasterVolume 2.0 ]   // 2.0 clamps to 1.0
#   ev.Requested   // -> [PlaySfx(SoundId "fire",0.8); PlayMusic(TrackId "level1",true); SetMasterVolume 1.0]
```

**Expected**: requests recorded in dispatch order; out-of-range volume clamped, no exception; no
IO/device access. Automated equivalent:

```sh
dotnet test tests/Canvas.Tests/Canvas.Tests.fsproj
```

**Expected**: new semantic tests fail before the surface exists and pass after — they assert the
exact `AudioEvidence.Requested` sequence for a set of game events driven through a pure model, with
zero IO in `update` (SC-001, SC-002).

## 2. Skill materialization gating (US3)

Instantiate the template for an audio profile and a non-audio profile, and diff the skill roots:

```sh
# game profile -> fs-gg-audio present
dotnet new fs-gg-ui -o /tmp/243-game  --profile game
ls /tmp/243-game/.agents/skills/fs-gg-audio/SKILL.md          # expected: present

# app profile -> fs-gg-audio absent
dotnet new fs-gg-ui -o /tmp/243-app   --profile app
ls /tmp/243-app/.agents/skills/fs-gg-audio 2>/dev/null || echo "absent (expected)"
```

**Expected**: exactly one `fs-gg-audio` skill for `game`/`sample-pack`, zero for
`app`/`headless-scene`/`governed` (SC-003, SC-005). Non-audio profiles are byte-unchanged w.r.t.
audio.

## 3. Manifest / template / parity coherence

```sh
dotnet fsi scripts/generate-skill-manifest.fsx        # regenerates fs-gg-audio sha256; must be a no-op diff after commit
dotnet test tests/Package.Tests/Package.Tests.fsproj  # Feature231SkillManifestTests + SurfaceAreaTests
```

**Expected**:
- The manifest `fs-gg-audio` entry's `sha256` matches the committed SKILL.md body; regenerating is
  a no-op (SC-003, SC-004).
- `materializes-when` (manifest) and the `condition` (template.json) carry the **same**
  `profile in [game, sample-pack]` predicate; the SkillParity harness passes (wrappers present in
  both `.agents/` and `.claude/`, canonical body referenced, no framework-process vocabulary leak).

## 4. Surface baseline (Principle II guard)

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx     # must be a no-op diff after the baseline is committed
dotnet test tests/Package.Tests/Package.Tests.fsproj # SurfaceAreaTests
```

**Expected**: `readiness/surface-baselines/FS.GG.UI.Canvas.txt` includes the new `Audio`
module/types and the surface-drift gate is green.

## 5. Skill currency / de-leak

**Expected**: every audio API the skill references resolves to shipped surface (no dangling
references), and the skill passes the existing skill-currency / de-leak checks — consumer
vocabulary only, no framework-process terms (SC-004).
