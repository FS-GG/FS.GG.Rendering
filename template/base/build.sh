#!/usr/bin/env bash
# Feature 212: uniform product-root verb wrapper. Every verb delegates to the single FAKE entry
# (`dotnet fsi build.fsx -t <Target>`), so FAKE stays the one rich/governed path. Stock
# `dotnet build/test/run` at the product root remain independently usable via <Name>.slnx.
# Mirrors the existing fake.sh style; parity with build.cmd.
set -euo pipefail

print_usage() {
    echo "Usage: ./build.sh <verb>" >&2
    echo "Supported verbs: restore build test run verify pack (help)" >&2
}

# Feature 242 (§2.3): the load-bearing build-target semantics, surfaced at the entry point so a
# green Dev is never mistaken for a passing compile. Kept in sync with docs/product.md + build.fsx.
# Requested help is success output → stdout (unlike print_usage's error text, which goes to stderr),
# matching build.fsx's stdout banner so `./build.sh --help | grep ...` works.
print_help() {
    cat <<'BANNER'
FS.GG.UI generated product — build targets
  Invoke: ./build.sh <verb> | dotnet fsi build.fsx -t <Target> | ./fake.sh -t <Target>

  Dev      A completion-marker / log-writer only — writes readiness/logs/Dev.txt. It does not compile
           your code; a green Dev is not evidence the build passes. Use Test for real feedback.
  Test     The first real compile + `dotnet test` (audit-free). Use this mid-implementation.
  Verify   Runs the merge-gate audit (EvidenceGraph -> EvidenceAudit) first — the audit hard-blocks
           until every task is [X] — then runs the tests. Use only when the feature is complete.

  Restore | Build | Run | Pack   Pass-through to stock dotnet over the single root .slnx.
BANNER
}

case "${1:-}" in
    restore) target=Restore ;;
    build)   target=Build ;;
    test)    target=Test ;;
    run)     target=Run ;;
    verify)  target=Verify ;;
    pack)    target=Pack ;;
    -h|--help|help) print_help; exit 0 ;;
    "")      echo "build.sh: missing verb" >&2; print_usage; exit 2 ;;
    *)       echo "build.sh: unknown verb '${1}'" >&2; print_usage; exit 2 ;;
esac

exec dotnet fsi build.fsx -t "$target"
