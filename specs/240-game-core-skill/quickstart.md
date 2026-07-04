# Quickstart — validate `fs-gg-game-core` end-to-end

Prerequisites: repo builds; `dotnet` + `fsi` available. Run from repo root.

## 1. Regenerate the manifest and prove no drift

```sh
dotnet fsi scripts/generate-skill-manifest.fsx            # rewrites template/skill-manifest/skill-manifest.json
dotnet fsi scripts/generate-skill-manifest.fsx --check    # expect: up-to-date, 13 entries
```

Expected: `skill-manifest.json` now has a `fs-gg-game-core` entry (between `fs-gg-feedback-capture` and
`fs-gg-keyboard-input`) with `materializes-when = (profile == "game" || profile == "sample-pack")` and
`supplied-by = template/product-skills/fs-gg-game-core/`; the twelve prior entries are unchanged
(`git diff` shows only the added block).

## 2. Run the skill Package.Tests (fail before → pass after)

```sh
dotnet test tests/Package.Tests/Package.Tests.fsproj \
  --filter "Feature219|Feature224|Feature225|Feature231|Feature238"
```

Expected green:
- `Feature231` — 13-entry catalog, digest matches.
- `Feature238` — `materializes-when` matches the `template.json` source; evaluates true/false per profile.
- `Feature219` — `game` & `sample-pack` rows include `fs-gg-game-core`; product-skill sources = 10.
- `Feature225` — `expectedProductSkillIds` (10) includes the new id; the body passes vocabulary/leak checks.
- `Feature224` — the referenced id resolves to a real skill (no dangle).

## 3. Surface-referenced check (no dangling member)

The added test asserts every FS.GG.UI member named in the body exists in the packed `.fsi`:

```sh
# members cited in the body must appear in:
#   template/base/docs/api-surface/Scene/Scene.fsi   (Geometry.*)
#   template/base/docs/api-surface/Canvas/*.fsi       (Rng.*, FixedStep.*)
```

Expected: pass; deliberately renaming a cited member in the body fails it.

## 4. Scaffold smoke — the skill materializes for game, not app

```sh
# game profile → skill present, byte-equal to source
dotnet new fs-gg-ui -o /tmp/gc-game --profile game    # (use the repo's scaffold entrypoint)
diff /tmp/gc-game/.agents/skills/fs-gg-game-core/SKILL.md \
     template/product-skills/fs-gg-game-core/SKILL.md   # expect: identical

# app profile → skill absent, but manifest still declares it
dotnet new fs-gg-ui -o /tmp/gc-app --profile app
test ! -e /tmp/gc-app/.agents/skills/fs-gg-game-core   # expect: true (absent)
```

Expected: present under `game` (and `sample-pack`), absent under `app`/`headless-scene`/`governed`; the
manifest declares the entry in all cases (declared ∧ condition-false ∧ absent is legitimate).

## 5. Consumer snippet compiles (Principle I / SC-004)

Paste the body's end-to-end snippet into an `.fsx` referencing the packed `Scene`/`Canvas` and run it —
the loop drains, the RNG threads its `next` state, the collision and cull evaluate — proving the body
talks to the real Feature-239 surface.

Done when steps 1–5 pass and `git diff` on the twelve prior manifest entries is empty.
