#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
node "$root/tests/Scene.SvgBrowser.Tests/scale-contract-test.mjs" >"${1:-/tmp/svg-scale-contract.json}"
for mutant in idle-rebuild world-node-growth world-cost-growth ordinary-latency dense-latency missed-frame retained-resource; do
  log="${TMPDIR:-/tmp}/svg-scale-$mutant.log"
  if node "$root/tests/Scene.SvgBrowser.Tests/scale-contract-test.mjs" --mutant "$mutant" >"$log" 2>&1; then
    echo "scale measurement mutant survived: $mutant" >&2
    exit 1
  fi
  grep -F "scale-contract:$mutant" "$log" >/dev/null
done
echo "svg-scale-contract: pass"
