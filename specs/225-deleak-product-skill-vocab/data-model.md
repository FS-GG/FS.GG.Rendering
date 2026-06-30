# Phase 1 Data Model: De-leak Product Skill Vocabulary

No persistent storage. Entities are **conceptual records** over shipped Markdown content and the
guard's in-memory scan — there is no database, no schema migration. The "data" is the skill bodies
and the findings the guard derives from them.

---

## Entity: Product skill

A shipped `template/product-skills/<id>/SKILL.md` an author receives on scaffold.

| Field | Description |
|---|---|
| `Id` / `SkillName` | The `name:` front-matter id (e.g. `fs-gg-testing`). Untouched by this feature. |
| `Description` | The `description:` front-matter line. Untouched (parity invariant, FR-006). |
| `Body` | The Markdown prose below front-matter — **the only thing this feature edits**. |
| `Path` | Repo-relative path; the guard filters on `Path.Contains("template/product-skills")`. |
| `CanonicalLineage` | Optional: the canonical framework skill this is vendored from, if any (drives parity). |

**Source of truth**: the on-disk file, enumerated via `SkillParity.inventorySkills` (which already
exposes `SkillName`, `Path`, `Content`).
**Validation rule**: `Body` MUST contain no Class-A/Class-C leak token and no **unconditional**
Class-B feedback reference (see Leak token). Front-matter MUST be byte-identical pre/post-edit.
**State transition**: `leaky → reframed` — each edit removes/generalizes framing while preserving the
lesson (FR-004 / SC-004); never `leaky → removed-lesson`.

---

## Entity: Leak token (three classes)

A string in product prose that only makes sense in the framework repo. Three classes, two
enforcement modes (banned outright vs conditional).

| Field | Description |
|---|---|
| `Class` | `A` framework-evidence-process · `B` lifecycle-feedback-path · `C` feature/spec-number-stamp |
| `Pattern` | The recognizer (literal or regex) the guard matches against a body line |
| `Mode` | `Banned` (any match is a finding) · `ConditionalOnSpecKit` (a match is a finding only when ungated) |

| Class | Patterns | Mode |
|---|---|---|
| A | `refresh-local-feed-and-samples`, `package-feed`, `specs/.*/readiness`, `.gitignore` (allowlist context), `BaseOutputPath` | Banned |
| B | `specs/.*/feedback` | ConditionalOnSpecKit — a finding **only** if its paragraph lacks a `spec kit`/`spec-kit` gating phrase (research R2) |
| C | `[Ff]eature\s+\d+`, `spec-\d+` | Banned (note: `spec-\d+` does not match `spec-kit`) |

**Source of truth**: the contract `contracts/leak-guard-check.md` (the authoritative pattern list).
**Validation rule**: a body line matching a `Banned` pattern, or a `ConditionalOnSpecKit` pattern
without a paragraph gating phrase, yields a Finding.
**State transition**: N/A (a token is either present or not on a given line).

---

## Entity: Lifecycle

The scaffold lifecycle of the produced product; determines which paths actually exist.

| Field | Description |
|---|---|
| `Kind` | `spec-kit` · `app` · `game` · `sdd` · `none` |
| `HasSpecsFeedback` | Whether `specs/<feature>/feedback/` exists (true only under `spec-kit`) |

**Source of truth**: the `dotnet new` template profile wiring; confirmed by the produced-surface run
(research R1).
**Validation rule**: under any `Kind` ≠ `spec-kit`, every "where to record findings" instruction in
every shipped skill MUST resolve to a real location (SC-002).
**State transition**: N/A (a property of the produced product, not mutated by this feature).

---

## Entity: Leak guard / Finding

The repo-owned check and the records it emits.

| Field | Description |
|---|---|
| `Skill` | The offending product-skill id |
| `Class` | The leak class (`A`/`B`/`C`) matched |
| `Token` | The matched leak string (for the message) |
| `File` | Repo-relative skill path |
| `Line` | 1-based line number of the match |
| `Message` | Formatted: skill id + class + matched token + `file:line` (FR-007 / VI) |

**Source of truth**: derived in-memory by scanning each Product skill `Body` against every Leak token.
**Validation rule**: the gate passes iff the Finding list over the real shipped set is empty
(SC-005, positive direction); it MUST be non-empty when any leak class is injected (negative
direction).
**State transition**: `scan → Finding list → assert empty`.

---

## Relationships

```text
Lifecycle ──determines──> which paths exist (HasSpecsFeedback)
   │
   ▼
Product skill (×7) ──contains──> Body
   │                                 │
   │                                 ▼
   │                          Leak token (Class A/B/C, banned|conditional)
   ▼                                 │
Leak guard ──scans Body for──────────┘──emits──> Finding (Skill, Class, Token, File:Line)
   │
   └──reuses──> SkillParity discovery (defaultRequest / discoverDefaultSurfaces / inventorySkills)
```

## Out of scope

- The consumer skill catalog docs (`skillist-reference.md` / `scaffold-map.md`) — owned by sibling
  **#36 / Feature 224**; not touched here (FR-005).
- Skill capability/behavior, adding/removing skills, front-matter — untouched (FR-005, FR-006).
- The new theming skill **#38** and the symbology delivery **#35** — separate items (spec
  Assumptions).
- The republish itself and the FS-GG/FS.GG.Templates#8 pin bump — this feature records the
  dependency (FR-008) but does not own the publish.
