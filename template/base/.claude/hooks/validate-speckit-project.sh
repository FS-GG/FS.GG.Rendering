#!/usr/bin/env bash
set -euo pipefail

project_dir="${CLAUDE_PROJECT_DIR:-$(pwd)}"

if [ ! -f "$project_dir/AGENTS.md" ]; then
  echo "AGENTS.md is missing; Spec Kit project guidance is unavailable." >&2
  exit 1
fi

if [ -f "$project_dir/.specify/feature.json" ]; then
  feature_dir="$(grep -o '"feature_directory"[[:space:]]*:[[:space:]]*"[^"]*"' "$project_dir/.specify/feature.json" | sed 's/.*"feature_directory"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' | head -n 1)"
  if [ -n "$feature_dir" ] && [ ! -f "$project_dir/$feature_dir/plan.md" ]; then
    echo "Active feature plan is missing: $feature_dir/plan.md" >&2
    exit 1
  fi
fi

exit 0
