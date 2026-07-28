# shellcheck shell=bash
# args.sh — argument-parsing guards shared across scripts/ (FS-GG/.github#356).
#
# A SOURCED fragment, not an executable: no shebang, no top-level effects. Source it AFTER
# defining `die`. The guards report through the caller's own `die`, so each script keeps its
# own message prefix and exit code — the part that is legitimately per-script — while sharing
# the guard body that was not:
#
#   die() { echo "myscript: $*" >&2; exit 2; }
#   . "$(dirname "${BASH_SOURCE[0]}")/lib/args.sh"
#
# need_val was byte-identical in coordination-sync, repos-audit.sh, and skill-union-assert.sh
# before it was hoisted here (#356); the point is ONE place a change to its semantics lands,
# rather than four copies where the next editor finds three and misses the fourth.
#
# scripts/repos.sh deliberately does NOT source this: its need_val takes a <subcommand> as its
# first argument (see the note at its own definition) and is a different function, not a copy.

# need_val <flag> <value…> — a flag given no value is a misconfiguration, not a verdict. Call it
# as `need_val "$@"` from a flag's case arm, before consuming "$2". Reporting through `die` avoids
# `${2:?…}`, whose exit 1 these scripts each reserve for a real finding (drift / an unwired
# receiver / a union violation) — i.e. a verdict rendered without reading a single file.
need_val() { [ $# -ge 2 ] && [ -n "${2:-}" ] || die "$1 needs a value."; }
