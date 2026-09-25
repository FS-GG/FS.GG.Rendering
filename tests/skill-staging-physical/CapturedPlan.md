# Descriptor-captured skill copy plan

Run from the repository root:

```sh
dotnet fsi tests/skill-staging-physical/CapturedPlanTests.fsx
```

`SkillStagingCapturedPlan.fsx` reads caller-supplied schema-v2 manifest bytes,
captures each product source through #1335's Linux descriptor reader, validates
its closed paths and canonical digests through #1331's pure policy, and owns
copies of the captured raw bytes. It derives each `skills/<id>/<path>`
destination and records the source file mode observed with the stable descriptor
read. Manifest file paths must exactly match captured spellings, so a normalized
backslash alias refuses before plan construction. `verifyProjection` refuses a
proposed copy path, mode, or byte change.
`verifyCurrent` compares the original manifest bytes and freshly captures the
source before a later use; changed bytes, paths, modes, and manifest bytes
refuse. The test also checks the committed 20-product catalog.

This is a read-only plan and check, with no output adapter. The manifest bytes
are supplied by the caller; this script does not pin a manifest file descriptor
or make all product-root captures atomic. A source can change after the last
fresh check, and adversarial ABA or concurrent in-place mutation can evade
bounded observations. File mode is captured source metadata, not a verified
staged or installed mode. The stacked [directory plan](DirectoryMode.md) now
records source directory paths and modes, including empty directories. The
stacked [output mode contract](OutputMode.md) now projects manifest and
container modes under an explicit umask. A copier must consume the plan's
owned bytes and verify its complete output before any parity or receiver claim.

Rendering #1333 is a release-preflight reducer, not a skill copy plan, and is
untouched here. #1332 Python preflight, #1334/#1335 physical source acceptance,
and installed-package parity remain separate prerequisites. No package
publication, release, BOM update, merge, or receiver pin is authorized.
