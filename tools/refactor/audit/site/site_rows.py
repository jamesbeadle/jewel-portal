"""The rows the site definition is reported in: each route as a user sees it, each component of the site with its definition."""
from __future__ import annotations

import re

from ..source_files import SourceFile, matchesAny
from .definition_kinds import Definition, flatten
from .notation import renderExpanded, renderStacked

ROUTE_DIRECTIVE = re.compile(r'^\s*@page\s+"([^"]+)"', re.MULTILINE)
ROUTE_FOLDER, ROUTE_FILE = "routes/", "/+page."


def routesOf(view: SourceFile, settings: dict) -> list[str]:
    text = "\n".join(view.lines)
    declared = ROUTE_DIRECTIVE.findall(text)
    if declared:
        return declared
    if matchesAny(view.relative, settings.get("routeGlobs", [])) and ROUTE_FOLDER in view.relative:
        folder = view.relative.split(ROUTE_FOLDER, 1)[1]
        return ["/" + folder.split(ROUTE_FILE, 1)[0].rstrip("/")]
    return []


def usedBy(name: str, definitionsByFile: dict[str, list[Definition]]) -> int:
    return sum(1 for definitions in definitionsByFile.values() if any(part.name == name for part in flatten(definitions)))


def routeRows(views: list[SourceFile], definitionsByFile: dict, parts: dict, settings: dict) -> list[dict]:
    rows = [
        {"route": route, "file": view.relative, "definition": renderStacked(definitionsByFile[view.relative]),
         "expanded": renderExpanded(definitionsByFile[view.relative], parts)}
        for view in views
        for route in routesOf(view, settings)
    ]
    return sorted(rows, key=lambda row: row["route"])


def componentRows(parts: dict[str, list[Definition]], files: dict[str, SourceFile], definitionsByFile: dict) -> list[dict]:
    rows = [
        {"component": name, "file": files[name].relative, "definition": renderStacked(definitions), "usedBy": usedBy(name, definitionsByFile)}
        for name, definitions in parts.items()
    ]
    return sorted(rows, key=lambda row: (-row["usedBy"], row["component"]))
