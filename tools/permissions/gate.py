"""Ratchet gate: fails when a permission rule is broken in more places than the baseline records.

Usage: python3 -m tools.permissions.gate tools/permissions/baseline.json <current.json>
Exit code 1 on any regression."""
from __future__ import annotations

import json
import sys
from pathlib import Path


def readSummaries(path: Path) -> dict:
    return json.loads(path.read_text())["summaries"]


def compare(baseline: dict, current: dict) -> list[str]:
    regressions = []
    for rule, baselineCount in baseline.items():
        currentCount = current.get(rule)
        if currentCount is None or currentCount <= baselineCount:
            continue
        regressions.append(f'"{rule}" broken in more places: {baselineCount} -> {currentCount}')
    return regressions


def main() -> int:
    baselinePath, currentPath = Path(sys.argv[1]), Path(sys.argv[2])
    regressions = compare(readSummaries(baselinePath), readSummaries(currentPath))
    if not regressions:
        print("Permission gate passed: no rule is broken in more places than the baseline.")
        return 0
    print("Permission gate FAILED:")
    for regression in regressions:
        print(f"  {regression}")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
