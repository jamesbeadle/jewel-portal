"""The widgets' designs: which catalogue widgets have a design sheet, and when the site was last checked against them.

Read from the design index the repository keeps (docs/design/widgets.json unless rules.json says otherwise). Nothing here is
scored: a design is a reference the person supplies, and checking the site against it is a judgement a person's Claude makes
on request, so the audit only reports where that stands.
"""
from __future__ import annotations

import json
from pathlib import Path

INDEX_WHEN_UNSET = "docs/design/widgets.json"
NO_INDEX, NEVER_CHECKED = "not set up", "never"
SHEET, BRAND = "design sheet", "brand only"


def indexPath(repositoryRoot: Path, settings: dict) -> Path:
    return repositoryRoot / settings.get("designIndex", INDEX_WHEN_UNSET)


def readIndex(repositoryRoot: Path, settings: dict) -> dict | None:
    path = indexPath(repositoryRoot, settings)
    if not path.exists():
        return None
    return json.loads(path.read_text())


def widgetRow(name: str, entry: dict, repositoryRoot: Path) -> dict:
    sheet = entry.get("sheet", "")
    hasSheet = bool(sheet) and (repositoryRoot / sheet).exists()
    return {
        "widget": name, "design": entry.get("design", []), "sheet": sheet if hasSheet else "",
        "standing": SHEET if hasSheet else BRAND, "checkedAt": entry.get("checkedAt", ""), "checkedAgainst": entry.get("checkedAgainst", ""),
    }


def lastChecked(rows: list[dict]) -> str:
    dates = sorted(row["checkedAt"] for row in rows if row["checkedAt"])
    return dates[-1] if dates else NEVER_CHECKED


def designs(repositoryRoot: Path, settings: dict, catalogue: list[str]) -> dict:
    index = readIndex(repositoryRoot, settings)
    relativeIndex = settings.get("designIndex", INDEX_WHEN_UNSET)
    if index is None:
        return {
            "index": relativeIndex, "isSetUp": False, "brand": "", "brandCheckedAt": NO_INDEX, "widgets": [], "sheets": 0,
            "lastChecked": NO_INDEX,
        }
    entries = index.get("widgets", {})
    rows = [widgetRow(name, entries.get(name, {}), repositoryRoot) for name in catalogue]
    return {
        "index": relativeIndex, "isSetUp": True, "brand": index.get("brand", ""), "brandCheckedAt": index.get("brandCheckedAt") or NEVER_CHECKED,
        "widgets": rows, "sheets": sum(1 for row in rows if row["standing"] == SHEET), "lastChecked": lastChecked(rows),
    }
