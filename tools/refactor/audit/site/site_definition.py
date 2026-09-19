"""The site definition: every routed view and every component of the site written in the widget notation, and the markup written by hand where a widget should be."""
from __future__ import annotations

from collections import Counter
from pathlib import Path

from ..source_files import SourceFile, matchesAny
from .definition import reduceView
from .definition_kinds import FINDING, WIDGET, Definition, flatten
from .markup_tree import buildTree
from .site_designs import designs
from .site_rows import componentRows, routeRows
from .vocabulary import vocabularyFor

NAME = "siteDefinition"
NOT_MEASURED = {"name": NAME, "summary": {"skipped": "no siteDefinition catalogue in rules.json"}, "offenders": {"handRolled": []}}


def findingRows(view: SourceFile, definitions: list[Definition]) -> list[dict]:
    return [
        {"file": view.relative, "line": definition.line, "element": definition.name, "ownedBy": definition.condition}
        for definition in flatten(definitions)
        if definition.kind == FINDING
    ]


def widgetUsages(definitions: list[Definition]) -> int:
    return sum(1 for definition in flatten(definitions) if definition.kind == WIDGET)


def byWidget(findings: list[dict]) -> list[dict]:
    counts = Counter(row["ownedBy"] for row in findings)
    return [
        {"ownedBy": owner, "count": count, "files": sorted({row["file"] for row in findings if row["ownedBy"] == owner})}
        for owner, count in counts.most_common()
    ]


def definitionOf(view: SourceFile, vocabulary) -> list[Definition]:
    isCatalogueWidget = Path(view.relative).stem in vocabulary.catalogue
    return reduceView(buildTree("\n".join(view.lines)), vocabulary, isCatalogueWidget)


def repositoryRootOf(view: SourceFile) -> Path:
    depth = len(Path(view.relative).parts)
    return Path(*view.path.parts[: len(view.path.parts) - depth])


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    settings = rules.get("siteDefinition", {})
    views = [file for file in sourceFiles if matchesAny(file.relative, settings.get("viewGlobs", []))]
    if not settings.get("catalogue") or not views:
        return NOT_MEASURED
    vocabulary = vocabularyFor(settings, views)
    definitionsByFile = {view.relative: definitionOf(view, vocabulary) for view in views}
    parts = {name: definitionsByFile[file.relative] for name, file in vocabulary.componentFiles.items()}
    findings = [row for view in views for row in findingRows(view, definitionsByFile[view.relative])]
    routes = routeRows(views, definitionsByFile, parts, settings)
    usages = sum(widgetUsages(definitions) for definitions in definitionsByFile.values())
    widgetDesigns = designs(repositoryRootOf(views[0]), settings, sorted(vocabulary.catalogue))
    return {
        "name": NAME,
        "summary": {
            "routes": len(routes), "views": len(views), "siteComponents": len(parts), "catalogue": len(vocabulary.catalogue),
            "widgetUsages": usages, "handRolledElements": len(findings), "widgetSlots": usages + len(findings),
            "viewsWithHandRolled": len({row["file"] for row in findings}),
            "designSheets": widgetDesigns["sheets"], "designsLastChecked": widgetDesigns["lastChecked"],
            "brandCheckedAt": widgetDesigns["brandCheckedAt"],
        },
        "designs": widgetDesigns,
        "offenders": {"handRolled": findings, "byWidget": byWidget(findings)},
        "catalogueNames": sorted(vocabulary.catalogue),
        "routes": routes,
        "components": componentRows(parts, vocabulary.componentFiles, definitionsByFile),
    }
