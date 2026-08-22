"""Builds the abstract page and widget inventory the taxonomy stage works from."""
from __future__ import annotations

from pathlib import Path

from ..source_files import SourceFile


def selectByGlob(sourceFiles: list[SourceFile], pattern: str) -> list[SourceFile]:
    import fnmatch
    directChildPattern = pattern.replace("**/", "")
    return [
        file
        for file in sourceFiles
        if fnmatch.fnmatch(file.relative, pattern)
        or fnmatch.fnmatch(file.relative, directChildPattern)
    ]


def widgetUsage(page: SourceFile, widgetMarkers: dict) -> list[str]:
    body = "\n".join(page.lines)
    return [widget for widget, marker in widgetMarkers.items() if marker in body]


def componentReuse(component: SourceFile, sourceFiles: list[SourceFile]) -> int:
    tag = "<" + Path(component.relative).stem
    return sum(
        1
        for file in sourceFiles
        if file.relative != component.relative and tag in "\n".join(file.lines)
    )


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    inventoryRules = rules["inventory"]
    pages = selectByGlob(sourceFiles, inventoryRules["pagesGlob"])
    components = selectByGlob(sourceFiles, inventoryRules["componentsGlob"])
    widgetMarkers = inventoryRules["widgetMarkers"]
    pageRows = [
        {"page": page.relative, "lines": page.lineCount, "widgets": widgetUsage(page, widgetMarkers)}
        for page in sorted(pages, key=lambda page: page.lineCount, reverse=True)
    ]
    reuseRows = sorted(
        (
            {"component": component.relative, "reusedBy": componentReuse(component, sourceFiles)}
            for component in components
        ),
        key=lambda row: row["reusedBy"],
        reverse=True,
    )
    orphanComponents = [row["component"] for row in reuseRows if row["reusedBy"] == 0]
    return {
        "name": "inventory",
        "summary": {
            "pages": len(pages),
            "components": len(components),
            "orphanComponents": len(orphanComponents),
            "averagePageLines": round(sum(page.lineCount for page in pages) / max(len(pages), 1)),
        },
        "offenders": {
            "pages": pageRows,
            "componentReuse": reuseRows,
            "orphans": orphanComponents,
        },
    }
