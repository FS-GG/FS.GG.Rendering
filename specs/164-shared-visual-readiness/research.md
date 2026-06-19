# Research: Shared Visual Readiness Tooling

## R1 - Public API Placement

**Decision**: Add the reusable model and helpers to `FS.GG.UI.Testing` through `src/Testing/Testing.fsi`, implemented in `src/Testing/Testing.fs`, with modules named around the domain: `VisualCaptureMatrix`, `VisualCompleteness`, `VisualReviewerClassifications`, `VisualReadiness`, and `VisualReadinessMarkdown`.

**Rationale**: `FS.GG.UI.Testing` already exposes generated-product validation helpers, screenshot evidence validators, readiness report models, and SkiaSharp-backed PNG artifact checks. The testing skill also defines this package as the owner of testing helper contracts and surface baselines.

**Alternatives considered**: Keeping the workflow in AntShowcase was rejected because it duplicates generic readiness logic. Moving it to `tests/Rendering.Harness` was rejected because package-consuming samples and generated products need a shipped helper surface. Creating a new package was rejected because the existing Testing package already has the dependency and contract role.

## R2 - Dependency Boundary

**Decision**: Introduce no new dependency. PNG completeness validation may use the existing `SkiaSharp` reference in `FS.GG.UI.Testing`. Contact-sheet image composition stays in AntShowcase app code or a future optional adapter unless a later implementation plan explicitly justifies moving it.

**Rationale**: The spec requires shared evidence logic, not shared image composition. The retrospective specifically warns against forcing SkiaSharp-specific contact-sheet composition into every test consumer. The Testing package already references SkiaSharp, so decoding PNG dimensions and content facts adds no new package burden.

**Alternatives considered**: Moving all contact-sheet generation into Testing was rejected because it expands the shared package's image-composition responsibility. Adding an image library was rejected because SkiaSharp is already available and pinned.

## R3 - Target Identity and Paths

**Decision**: Capture targets use stable ids derived from page id, theme id, size role/dimensions, and relative artifact path. Validation accepts an evidence root and target-relative paths, then records normalized relative path, byte count, decoded dimensions, and a stable SHA-256 content identity for existing artifacts.

**Rationale**: Relative paths let package-consuming samples produce portable reports. Content identity and byte count make stale or accidental reuse easier to detect without depending on wall-clock timestamps.

**Alternatives considered**: Absolute paths in reports were rejected as non-portable. Timestamp identity was rejected because it is unstable across machines and repeated runs.

## R4 - Completeness Classification

**Decision**: Classify each required artifact as `complete`, `missing`, `wrong-size`, `undecodable`, `degraded`, or `blocked`. Degraded records require a non-empty reason. Missing, wrong-size, undecodable, malformed, and blocking reviewer defects block accepted readiness. Degraded captures remain visible and cannot be counted as fully complete.

**Rationale**: The spec requires that degraded evidence be impossible to mistake for accepted screenshots. Existing AntShowcase behavior also treats real screenshots as the only accepted captures.

**Alternatives considered**: A single pass/fail result was rejected because it hides actionable distinctions reviewers need. Treating degraded capture as accepted with a caveat was rejected because it weakens visual evidence.

## R5 - Reviewer Classification

**Decision**: Generate reviewer templates with one row per required target. Parse rows back into records keyed by target id and classify missing, duplicate, malformed, unknown-target, minor, major, and blocking records. Readiness remains pending review until every required target has a non-pending classification.

**Rationale**: Feature 162 showed that screenshot presence alone is insufficient. Explicit human review prevents an image matrix from being accepted without reviewer judgement.

**Alternatives considered**: Matrix-level reviewer status was rejected because it cannot point back to target-specific defects. Free-form prose parsing was rejected because validation lanes need machine-checkable output.

## R6 - Summary Preservation

**Decision**: Generated visual-readiness content uses managed markers in manual summary files and generated-only files for machine reports. The managed writer recognizes exactly one start marker and one end marker. Missing markers are inserted at a deterministic location. Multiple, reversed, or incomplete markers fail with an actionable diagnostic and do not modify the file.

**Rationale**: The retrospective records that a generated visual summary overwrote richer manual validation notes. Safe marker handling lets regeneration update only the shared section.

**Alternatives considered**: Rewriting whole summary files was rejected because it repeats the original failure mode. Requiring all summaries to be generated-only was rejected because feature readiness summaries often need manual package-validation notes and caveats.

## R7 - AntShowcase Migration

**Decision**: Migrate AntShowcase to shared Testing APIs for matrix expansion, generic capture record aggregation, reviewer template/parsing, readiness decision, JSON/Markdown summary content, and managed-section updates. Keep page registry, accepted size definitions, Ant theme aliases, `Viewer.captureScreenshotEvidence`, and contact-sheet PNG rendering in AntShowcase.

**Rationale**: This preserves sample ownership of rendering and product decisions while removing generic readiness workflow duplication.

**Alternatives considered**: A full rewrite of AntShowcase visual readiness was rejected as unnecessary. Migrating screenshot capture into Testing was rejected because the shared package should not own sample rendering callbacks.

## R8 - Validation Commands and Evidence

**Decision**: Validate with focused Testing tests, AntShowcase parity tests, package surface checks, and local package validation:

- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj`
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"`
- `./fake.sh build -t CapabilityCheck`
- `./fake.sh build -t PackageSurfaceCheck`
- `./fake.sh build -t PackLocal`
- `./fake.sh build -t GeneratedProductCheck`

**Rationale**: The feature changes a public package and a package-consuming sample. Both the package contract and first adopter behavior need evidence.

**Alternatives considered**: Running only AntShowcase tests was rejected because the reusable package contract would be under-tested. Running only full solution validation was rejected because focused failure evidence is easier to diagnose.

## R9 - Compatibility and Migration

**Decision**: Make the Testing API additive for this feature. Update `readiness/surface-baselines/FS.GG.UI.Testing.txt` and document AntShowcase migration steps in the feature quickstart and readiness notes. Existing `EvidenceReports` screenshot validators remain supported.

**Rationale**: Existing generated products and tests should not break merely by adding shared visual-readiness helpers. Surface drift must still be intentional and reviewable.

**Alternatives considered**: Replacing existing screenshot evidence types was rejected because it would create avoidable migration cost.

