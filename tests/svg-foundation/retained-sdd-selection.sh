#!/usr/bin/env bash
# Closed release routes; arbitrary caller-supplied versions are not admitted.
select_retained_sdd() {
  sdd_selection="$1"
  case "$sdd_selection" in
    historical-1.7) sdd_version=1.7.0 ;;
    current-2.1) sdd_version=2.1.0 ;;
    *) echo "svg-retained-qualification: unsupported SDD selection: $sdd_selection" >&2; return 1 ;;
  esac
}

require_retained_sdd_identity() {
  [[ "$1" == "$sdd_version" ]]
}
