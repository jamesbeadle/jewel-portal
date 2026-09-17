"""Compares each entity's file count with what its complexity predicts.

An entity's properties are its complexity; the files named for it are what the design patterns made of it.
Across a consistent codebase the two keep a steady ratio, so an entity far outside it is where the pattern
was skipped or where the code was piled into too few files.
"""
from __future__ import annotations

import re
from statistics import median

from ..declarations import TYPE_DECLARATION
from ..source_files import SourceFile, matchesAny
from ..words import containsPhrase, wordsOf

PROPERTY_LINE = re.compile(r"^\s*(?:public\s+[\w<>\[\]\?,\s]+\s+\w+\s*\{\s*get|(?:readonly\s+)?\w+\??\s*:\s*[^=]+[;,]?\s*$)")
MINIMUM_ENTITIES = 4
FILE_COUNT_BAND = 2.0


def entityProperties(sourceFiles: list[SourceFile], entityGlobs: list[str]) -> dict[str, int]:
    properties: dict[str, int] = {}
    for sourceFile in sourceFiles:
        if not matchesAny(sourceFile.relative, entityGlobs):
            continue
        currentEntity = None
        for line in sourceFile.lines:
            declaration = TYPE_DECLARATION.match(line)
            if declaration:
                currentEntity = declaration.group(1)
                properties.setdefault(currentEntity, 0)
            if currentEntity and not declaration and PROPERTY_LINE.match(line):
                properties[currentEntity] += 1
    return {entity: count for entity, count in properties.items() if count > 0}


def fileCounts(sourceFiles: list[SourceFile], entities: list[str]) -> dict[str, int]:
    longestFirst = sorted(entities, key=lambda entity: len(wordsOf(entity)), reverse=True)
    counts = {entity: 0 for entity in entities}
    for sourceFile in sourceFiles:
        pathWords = wordsOf(sourceFile.relative)
        owner = next((entity for entity in longestFirst if containsPhrase(pathWords, wordsOf(entity))), None)
        if owner:
            counts[owner] += 1
    return counts


def measure(sourceFiles: list[SourceFile], patternRules: dict) -> dict:
    properties = entityProperties(sourceFiles, patternRules.get("entityGlobs", []))
    if len(properties) < patternRules.get("minimumEntities", MINIMUM_ENTITIES):
        return {"entities": 0, "entitiesOutOfRange": 0, "rows": []}
    counts = fileCounts(sourceFiles, list(properties))
    filesPerProperty = median(counts[entity] / properties[entity] for entity in properties)
    band = patternRules.get("fileCountBand", FILE_COUNT_BAND)
    rows = []
    for entity, propertyCount in sorted(properties.items()):
        expected = max(1, round(filesPerProperty * propertyCount))
        isInRange = expected / band <= counts[entity] <= expected * band
        rows.append({"entity": entity, "properties": propertyCount, "files": counts[entity], "expectedFiles": expected, "isInRange": isInRange})
    return {
        "entities": len(rows),
        "entitiesOutOfRange": sum(1 for row in rows if not row["isInRange"]),
        "filesPerProperty": round(filesPerProperty, 2),
        "rows": rows,
    }
