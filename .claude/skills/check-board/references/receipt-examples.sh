#!/usr/bin/env bash
set -euo pipefail

positive='{"subject":"FS.GG.SDD#47","writes":[{"field":"Status","value":"Ready"},{"field":"Blocked by","value":""}],"observed":[{"field":"Status","value":"Ready"},{"field":"Blocked by","value":""}],"outcome":"written","error":null}'
partial='{"subject":"FS.GG.SDD#47","writes":[{"field":"Status","value":"Ready"},{"field":"Blocked by","value":""}],"observed":[{"field":"Status","value":"Ready"},{"field":"Blocked by","value":"FS-GG/FS.GG.SDD#45"}],"outcome":"failed","error":"Blocked by remains stale"}'
missing='{"subject":"FS.GG.SDD#47","writes":[{"field":"Status","value":"Ready"},{"field":"Blocked by","value":""}],"observed":[],"outcome":"failed","error":"the item left the board before fresh verification"}'

printf '%s\n' "$positive" | jq -e '
  .outcome == "written" and .observed == .writes
' >/dev/null

printf '%s\n' "$partial" | jq -e '
  .outcome == "failed" and
  .writes == [{"field":"Status","value":"Ready"},{"field":"Blocked by","value":""}] and
  .observed == [{"field":"Status","value":"Ready"},{"field":"Blocked by","value":"FS-GG/FS.GG.SDD#45"}] and
  (.error | contains("Blocked by"))
' >/dev/null

printf '%s\n' "$missing" | jq -e '
  .outcome == "failed" and .observed == [] and
  (.error | contains("left the board"))
' >/dev/null

echo "receipt examples: positive, partial, and missing-row contracts verified"
