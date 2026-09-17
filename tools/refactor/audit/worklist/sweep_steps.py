"""The last pass of the plan: every remaining figure brought to zero, the lowest-scoring element first."""
from __future__ import annotations

from ..score.elements import ELEMENTS

SWEEP = "Pass 4 — The sweep to zero"
HANDLED_BY_EARLIER_PASSES = {"filesOverLimit", "worstFile", "orphans", "predictedFiles", "entityFileCounts"}


def checkNameFor(elementKey: str) -> str:
    return next(row["figures"][0][0] for row in ELEMENTS if row["key"] == elementKey)


def sweepSteps(score: dict) -> list[dict]:
    remaining = [
        row for row in score["elements"]
        if row["isMeasured"] and row["offenders"] and row["key"] not in HANDLED_BY_EARLIER_PASSES
    ]
    remaining.sort(key=lambda row: row["score"])
    return [
        {
            "pass": SWEEP,
            "title": f"{row['label']}: {row['offenders']:g} to zero",
            "detail": f"Scores {row['score']:.1f}% at weight {row['weight']}; the offenders are in audit.json under details.{checkNameFor(row['key'])}, fifty at a time.",
        }
        for row in remaining
    ]
