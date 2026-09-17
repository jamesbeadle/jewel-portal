"""Turns the audit's summaries into the code quality score."""
from __future__ import annotations

from .elements import THOUSAND_LINES, elementsFor

LINES_PER_THOUSAND = 1000
HIGHEST_IMPERFECT_SCORE = 99.9


def readFigures(summaries: dict, figures: list[tuple[str, str]]) -> float | None:
    values = [1 if check == "constant" else summaries.get(check, {}).get(figure) for check, figure in figures]
    if any(not isinstance(value, (int, float)) for value in values):
        return None
    return sum(values)


def measuredAgainst(summaries: dict, element: dict) -> float | None:
    denominator = readFigures(summaries, element["per"])
    if denominator and element["per"] == THOUSAND_LINES:
        return denominator / LINES_PER_THOUSAND
    return denominator


def scoreElement(summaries: dict, element: dict) -> dict:
    offenders = readFigures(summaries, element["figures"])
    denominator = measuredAgainst(summaries, element)
    row = {key: element[key] for key in ("key", "label", "group", "weight", "zeroAt", "unit")}
    if offenders is None or not denominator:
        return {**row, "isMeasured": False, "offenders": offenders, "score": None}
    density = offenders / denominator
    score = max(0.0, 1 - density / element["zeroAt"]) * 100
    return {**row, "isMeasured": True, "offenders": offenders, "measuredAgainst": round(denominator, 2), "score": round(score, 1)}


def weightedScore(rows: list[dict]) -> float | None:
    measuredRows = [row for row in rows if row["isMeasured"]]
    totalWeight = sum(row["weight"] for row in measuredRows)
    if not totalWeight:
        return None
    exact = sum(row["score"] * row["weight"] for row in measuredRows) / totalWeight
    hasOffenders = any(row["offenders"] for row in measuredRows)
    rounded = round(exact, 1)
    return min(rounded, HIGHEST_IMPERFECT_SCORE) if hasOffenders else rounded


def calculate(summaries: dict, rules: dict) -> dict:
    rows = [scoreElement(summaries, element) for element in elementsFor(rules)]
    groupNames = list(dict.fromkeys(row["group"] for row in rows))
    groups = [
        {"group": name, "score": weightedScore([row for row in rows if row["group"] == name]),
         "weight": sum(row["weight"] for row in rows if row["group"] == name and row["isMeasured"])}
        for name in groupNames
    ]
    return {"overall": weightedScore(rows), "groups": groups, "elements": rows}
