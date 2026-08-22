"""Ratchet gate: fails when quality regresses against the committed baseline.

Usage: python -m audit.gate baseline.json audit-output/audit.json
Exit code 1 on any regression; intended as a CI step after run_audit.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

RATCHETED_FIGURES = [
    ("fileLength", "filesOverLimit"),
    ("fileLength", "worstFileLines"),
    ("functionShape", "functionsOverLimit"),
    ("functionShape", "elseBlocks"),
    ("duplication", "duplicatedPercentage"),
    ("comments", "explanatoryCommentLines"),
    ("magicValues", "inlineHexColours"),
    ("inventory", "orphanComponents"),
]


def readSummaries(path: Path) -> dict:
    return json.loads(path.read_text())["summaries"]


def compare(baseline: dict, current: dict) -> list[str]:
    regressions = []
    for checkName, figureName in RATCHETED_FIGURES:
        baselineValue = baseline.get(checkName, {}).get(figureName)
        currentValue = current.get(checkName, {}).get(figureName)
        if baselineValue is None or currentValue is None:
            continue
        if currentValue > baselineValue:
            regressions.append(
                f"{checkName}.{figureName} regressed: {baselineValue} -> {currentValue}"
            )
    return regressions


def main() -> int:
    baselinePath, currentPath = Path(sys.argv[1]), Path(sys.argv[2])
    regressions = compare(readSummaries(baselinePath), readSummaries(currentPath))
    if not regressions:
        print("Gate passed: no figure regressed against the baseline.")
        return 0
    print("Gate FAILED:")
    for regression in regressions:
        print(f"  {regression}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
