"""Renders the score's breakdown as the markdown table the report and the README box share."""
from __future__ import annotations

THOUSAND_LINES_UNIT = "thousand lines"


def percentage(score: float | None) -> str:
    return "not measured" if score is None else f"{score:.1f}%"


def reading(row: dict) -> str:
    if not row["isMeasured"]:
        return "not measured"
    if not row["unit"]:
        return f"{row['offenders']:g}"
    return f"{row['offenders']:g} in {row['measuredAgainst']:g} {row['unit']}"


def zeroPoint(row: dict) -> str:
    if not row["unit"]:
        return f"{row['zeroAt']:g}"
    if row["unit"] == THOUSAND_LINES_UNIT:
        return f"{row['zeroAt']:g} per thousand lines"
    return f"{row['zeroAt']:.0%} of {row['unit']}"


def breakdownTable(score: dict) -> str:
    rows = ["| Element | Reading | Score | Weight | 0% at |", "| --- | --- | --- | --- | --- |"]
    for group in score["groups"]:
        rows.append(f"| **{group['group']}** | | **{percentage(group['score'])}** | **{group['weight']}** | |")
        rows += [
            f"| {row['label']} | {reading(row)} | {percentage(row['score'])} | {row['weight'] if row['isMeasured'] else '—'} | {zeroPoint(row)} |"
            for row in score["elements"]
            if row["group"] == group["group"]
        ]
    return "\n".join(rows)


def derivation() -> str:
    return (
        "Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, "
        "measured against the size of the codebase, reach the figure in the last column. The score is the "
        "weighted average of the elements that could be measured; an element that could not be measured "
        "lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under "
        "`score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`."
    )
