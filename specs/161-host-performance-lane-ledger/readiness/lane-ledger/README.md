# Feature 161 Lane Ledger

The lane ledger records every timing run considered for host-scoped P7 performance acceptance.

## Required Files

- `summary.md`: reviewer-readable ledger summary.
- `summary.json`: machine-readable run, lane, status, accepted-count, unsupported-host, and exclusion summary.
- `entries/`: one `entry-*.md` ledger entry per timing run.
- `host-facts/`: one `facts-*.md` host fact record per timing run.
- `excluded/`: grouped rejected evidence by primary reason.
- `unsupported/README.md`: unsupported-host record with accepted lane-scoped performance artifacts `0`.

Every accepted ledger entry must include display server, display identity, renderer identity, direct rendering status, refresh rate or unavailable reason, driver identity, package version set, CPU/GPU load notes, environment limits, host profile, run identity, scenario identity, timing policy identity, collection time, and artifact locations.
