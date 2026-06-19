<!-- SKILL-PARITY:START -->
# Feature 168 Guidance Coverage

Overall status: `warning`

| Rule | Covered | Partial | Missing | Excepted | Not applicable |
| --- | --- | --- | --- | --- | --- |
| package-pin-drift | 9 | 0 | 0 | 0 | 40 |
| readiness-allowlisting | 6 | 0 | 0 | 0 | 43 |
| validation-output-isolation | 4 | 0 | 0 | 0 | 45 |
| visual-readiness | 9 | 2 | 0 | 0 | 38 |
| responsiveness-diagnostics | 7 | 0 | 0 | 0 | 42 |
| post-merge-package-bump | 2 | 0 | 0 | 0 | 47 |
| evidence-honesty | 12 | 0 | 0 | 0 | 37 |

## Required Rules
- `package-pin-drift`: Package-consuming samples check current FS.GG.UI.* package pins and use local-feed proof.
- `readiness-allowlisting`: Committed readiness evidence is ignored by default until allowlisted.
- `validation-output-isolation`: Same project/configuration validation is not parallelized unless output paths are isolated.
- `visual-readiness`: Real screenshots, degraded capture disclosure, reviewer classification, and summary caveat preservation are required.
- `responsiveness-diagnostics`: Interactive readiness validates pointer and keyboard activation separately and separates routing from update/render/present latency.
- `post-merge-package-bump`: Merge/post-merge work records package bump, local-feed pack, sample pin alignment, restore/validation, and readiness ledger updates.
- `evidence-honesty`: Canceled, timed-out, skipped, synthetic, substitute, degraded, pending-review, and environment-limited checks are visibly caveated.
<!-- SKILL-PARITY:END -->
