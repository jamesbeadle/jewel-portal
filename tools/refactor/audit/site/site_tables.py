"""The tables of the site definition document: the components of the site, and the markup written by hand."""
from __future__ import annotations

SHOWN_FILES = 6


def componentTable(components: list[dict]) -> str:
    rows = ["| Component | Used by | Definition |", "| --- | --- | --- |"]
    rows += [f"| `{row['component']}` | {row['usedBy']} | {inCell(row['definition'])} |" for row in components]
    return "\n".join(rows)


def inCell(definition: str) -> str:
    if not definition:
        return "—"
    return "`" + definition.replace("|", "\\|") + "`"


def shortFiles(files: list[str]) -> str:
    extra = f" +{len(files) - SHOWN_FILES}" if len(files) > SHOWN_FILES else ""
    return ", ".join(f"`{file}`" for file in files[:SHOWN_FILES]) + extra


def handRolledTable(byWidget: list[dict]) -> str:
    if not byWidget:
        return "Nothing is written by hand where a widget should be."
    rows = ["| Should be | Written by hand | Where |", "| --- | --- | --- |"]
    rows += [f"| `{row['ownedBy']}` | {row['count']} in {len(row['files'])} files | {shortFiles(row['files'])} |" for row in byWidget]
    return "\n".join(rows)
