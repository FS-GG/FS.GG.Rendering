#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
source "$root/tests/svg-foundation/retained-sdd-selection.sh"
for selection in historical-1.7 current-2.1; do
  select_retained_sdd "$selection"
  require_retained_sdd_identity "$sdd_version"
  for observed in "${sdd_version}0" "${sdd_version}-preview.1" "fsgg-sdd $sdd_version" "prefix $sdd_version suffix"; do
    if require_retained_sdd_identity "$observed"; then
      echo "accepted lookalike identity: $observed" >&2
      exit 1
    fi
  done
done
for selection in 2.1.0 current-2.2 latest ''; do
  if select_retained_sdd "$selection" 2>/dev/null; then
    echo "accepted unreviewed selection: $selection" >&2
    exit 1
  fi
done
python3 "$root/tests/svg-foundation/test-compare-retained-authority.py"
