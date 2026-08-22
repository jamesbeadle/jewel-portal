"""Counts explanatory comments and unowned task markers."""
from __future__ import annotations

import re

from ..source_files import SourceFile

TASK_MARKER = re.compile(r"\b(TODO|FIXME|HACK)\b", re.IGNORECASE)


def isExplanatoryComment(strippedLine: str, markers: dict) -> bool:
    lineMarker = markers["line"]
    documentationMarker = markers["documentation"]
    if strippedLine.startswith(documentationMarker):
        return False
    return strippedLine.startswith(lineMarker) or strippedLine.startswith(markers["markup"])


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    markers = rules["commentMarkers"]
    commentCount = 0
    taskMarkers = []
    commentedFiles = set()
    for sourceFile in sourceFiles:
        for lineNumber, line in enumerate(sourceFile.lines, start=1):
            stripped = line.strip()
            if not isExplanatoryComment(stripped, markers):
                continue
            commentCount += 1
            commentedFiles.add(sourceFile.relative)
            if TASK_MARKER.search(stripped):
                taskMarkers.append({"file": sourceFile.relative, "line": lineNumber})
    return {
        "name": "comments",
        "summary": {
            "explanatoryCommentLines": commentCount,
            "filesWithComments": len(commentedFiles),
            "taskMarkers": len(taskMarkers),
        },
        "offenders": taskMarkers[:50],
    }
