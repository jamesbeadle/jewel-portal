#!/usr/bin/env bash
# How many commits have landed on this branch since tools/refactor/baseline.json was last
# committed — the measurable rhythm a refactor round runs on. Prints the count and says
# whether a round is due; exits 0 when it is, 1 when it is not. Usage:
#   tools/refactor/deploys_since_baseline.sh [every]     (every defaults to 10)
set -euo pipefail

EVERY="${1:-10}"
ROOT="$(git rev-parse --show-toplevel)"
BASELINE="tools/refactor/baseline.json"

BASELINE_COMMIT="$(git -C "$ROOT" log -1 --format=%H -- "$BASELINE" || true)"
if [ -z "$BASELINE_COMMIT" ]; then
  echo "No committed baseline yet: commit $BASELINE, then the count starts from there."
  exit 1
fi

COUNT="$(git -C "$ROOT" rev-list --count --first-parent "$BASELINE_COMMIT..HEAD")"
BASELINE_DATE="$(git -C "$ROOT" log -1 --format=%ad --date=short "$BASELINE_COMMIT")"
echo "$COUNT commit(s) on $(git -C "$ROOT" branch --show-current) since the baseline of $BASELINE_DATE ($(git -C "$ROOT" rev-parse --short "$BASELINE_COMMIT")); a round is due at $EVERY."

if [ "$COUNT" -ge "$EVERY" ]; then
  echo "A refactor round is due."
  exit 0
fi
echo "Not due yet."
exit 1
