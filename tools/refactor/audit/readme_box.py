"""Writes the code quality box at the bottom of the README: the score in a box, its breakdown expandable beneath.

Usage: python3 -m tools.refactor.audit.quality_check writes it; this module only renders and places it.
"""
from __future__ import annotations

import re
from pathlib import Path

from .inventory.file_areas import areasLine, areasTable
from .score.breakdown import breakdownTable, derivation, percentage
from .worklist.render import orderedSteps

BOX_START, BOX_END = "<!-- code-quality:start -->", "<!-- code-quality:end -->"
STEPS_IN_THE_BOX = 10
PLAN_PATH = "tools/refactor/refactor-plan.md"
EXISTING_BOX = re.compile(re.escape(BOX_START) + r".*?" + re.escape(BOX_END) + r"\n?", re.DOTALL)


def planSection(steps: list[dict]) -> list[str]:
    if not steps:
        return ["Nothing is left to refactor: every figure is at zero."]
    remaining = len(steps) - STEPS_IN_THE_BOX
    more = f"… and {remaining} more steps. " if remaining > 0 else ""
    return [orderedSteps(steps[:STEPS_IN_THE_BOX]), "", f"{more}The whole plan, with the measured detail, is in [`{PLAN_PATH}`]({PLAN_PATH})."]


def expandable(summary: str, body: list[str]) -> list[str]:
    return ["<details>", f"<summary><strong>{summary}</strong></summary>", "", *body, "", "</details>", ""]


def scoreBox(audit: dict) -> list[str]:
    return [
        '<table><tr><td align="center">',
        f"<strong>Code quality score</strong><h2>{percentage(audit['score']['overall'])}</h2>",
        f"<sub>measured {audit['generatedAt'][:10]} · project-process kit {audit.get('kitVersion', '')}</sub>",
        "</td></tr></table>",
        "",
    ]


def renderBox(audit: dict, steps: list[dict]) -> str:
    score = audit["score"]
    fileAreas = audit["details"]["fileAreas"]
    totalFiles = fileAreas["summary"]["totalFiles"]
    return "\n".join([
        BOX_START,
        "## Code quality",
        "",
        *scoreBox(audit),
        areasLine(fileAreas),
        "",
        *expandable(f"How the {percentage(score['overall'])} is made up", [breakdownTable(score), "", derivation()]),
        *expandable(f"The repository by area: {totalFiles:,} files", [areasTable(fileAreas)]),
        *expandable(f"The refactoring plan: {len(steps)} steps, in order", planSection(steps)),
        BOX_END,
        "",
    ])


def writeBox(readmePath: Path, audit: dict, steps: list[dict]) -> None:
    existing = readmePath.read_text() if readmePath.exists() else ""
    withoutBox = EXISTING_BOX.sub("", existing).rstrip("\n")
    separator = "\n\n" if withoutBox else ""
    readmePath.write_text(withoutBox + separator + renderBox(audit, steps))
