# Contract: fs-gg-layout consumer-skill delivery & gates

This feature exposes no code interface. Its "contract" is (a) what a scaffolded product receives and (b) the invariants the repo gates assert. Both are stated here as verifiable clauses.

## C1 — Product-receipt contract (what the consumer gets)

For a product scaffolded with profile ∈ {`app`, `game`} under lifecycle ∈ {`spec-kit`, `sdd`, `none`}:

- **MUST** contain `.agents/skills/fs-gg-layout/SKILL.md` and `.claude/skills/fs-gg-layout/SKILL.md`, byte-identical to `template/product-skills/fs-gg-layout/SKILL.md`.
- The `SKILL.md` `name:` frontmatter **MUST** equal `fs-gg-layout`.

For a product scaffolded with profile ∈ {`headless-scene`, `governed`, `sample-pack`}:

- **MUST NOT** contain `fs-gg-layout` under either agent surface.

*Proven by*: live scaffold (quickstart step 3) and the env-free Feature 219 derivation from `template.json`.

## C2 — Content-boundary contract (consumer slice only)

`template/product-skills/fs-gg-layout/SKILL.md`:

- **MUST** document: computing HUD + gameplay/content regions from output size; keeping an active item inside the gameplay region; the `LayoutEvidence` region/bounds shape the starter ships.
- **MUST NOT** document the Yoga layout-engine internals (`Layout.evaluate`, `Defaults.layoutNode`), `.fsi`/surface-baseline authoring, or any framework-owned pipeline internals — a `Boundary` section **MUST** name that exclusion and point authority to the framework `fs-gg-layout`.
- **MUST** pass the Feature 225 leak guard (no `Feature \d+` / `spec-\d+` / framework-evidence tokens).
- Every code example **MUST** correspond to the layout surface the `app`/`game` starter exposes (no invented API).

*Proven by*: Feature 225 leak gate + content hand-read (quickstart step 4).

## C3 — Enumeration-coherence contract (gates stay honest)

After this feature the shipped product-skill set is **9** (was 8) and the framework-skill source count is **18** (was 16). The following **MUST** hold simultaneously:

| Gate | Assertion |
|---|---|
| Feature 224 catalog currency | `fs-gg-layout` row present, resolves, `app, game` scope; no dangling/unlisted rows |
| Feature 225 leak guard | `expectedProductSkillIds` ⊇ includes `fs-gg-layout`; discovered ⊇ backstop; zero leak findings |
| Feature 219 emission matrix | `app` & `game` expected sets include `fs-gg-layout` (8 each); source floor `>=18`; every `fs-gg-layout` source lifecycle-independent + profile-predicated + both surfaces |
| Feature 204 lifecycle template | framework-source floor `>=18` |
| skill-parity | canonical↔wrapper paired; overall `Passed`; zero High+ findings; report regenerated |

*Proven by*: quickstart steps 1–2.

## C4 — Non-contract (explicitly out of scope)

- **No** `fs-gg-ui-template` version bump, tag triple, or registry flip — additive content under the existing contract (mirrors Feature 226). Consumer delivery is the separate epic-#34 republish.
- **No** `src/**` change, no new `.fsi`/surface-baseline, no runtime behavior change.
