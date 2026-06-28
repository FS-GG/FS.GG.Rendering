#!/usr/bin/env bash
# Feature 212: uniform product-root verb wrapper. Every verb delegates to the single FAKE entry
# (`dotnet fsi build.fsx -t <Target>`), so FAKE stays the one rich/governed path. Stock
# `dotnet build/test/run` at the product root remain independently usable via <Name>.slnx.
# Mirrors the existing fake.sh style; parity with build.cmd.
set -euo pipefail

print_usage() {
    echo "Usage: ./build.sh <verb>" >&2
    echo "Supported verbs: restore build test run verify pack" >&2
}

case "${1:-}" in
    restore) target=Restore ;;
    build)   target=Build ;;
    test)    target=Test ;;
    run)     target=Run ;;
    verify)  target=Verify ;;
    pack)    target=Pack ;;
    "")      echo "build.sh: missing verb" >&2; print_usage; exit 2 ;;
    *)       echo "build.sh: unknown verb '${1}'" >&2; print_usage; exit 2 ;;
esac

exec dotnet fsi build.fsx -t "$target"
