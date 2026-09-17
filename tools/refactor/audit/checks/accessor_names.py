"""Flags accessor functions that glue a type to one of its properties: getAppleColour wants to be apple.colour."""
from __future__ import annotations

from ..declarations import declaredFunctions, declaredTypeNames
from ..source_files import SourceFile
from ..words import wordsOf

ACCESSOR_VERBS_WHEN_UNSET = ["get", "fetch", "read"]
LOOKUP_WORDS_WHEN_UNSET = ["by", "for", "from", "in", "of", "with", "on", "at", "to", "as", "all", "or", "and"]


def longestTypePrefix(words: list[str], typeWords: set[tuple[str, ...]]) -> int:
    for length in range(len(words), 0, -1):
        if tuple(words[:length]) in typeWords:
            return length
    return 0


def isGluedAccessor(name: str, typeWords: set[tuple[str, ...]], nameRules: dict) -> bool:
    words = wordsOf(name)
    if len(words) < 3 or words[0] not in nameRules.get("accessorVerbs", ACCESSOR_VERBS_WHEN_UNSET):
        return False
    subject = words[1:]
    if any(word in nameRules.get("lookupWords", LOOKUP_WORDS_WHEN_UNSET) for word in subject):
        return False
    typeLength = longestTypePrefix(subject, typeWords)
    return 0 < typeLength < len(subject)


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    nameRules = rules.get("functionNames", {})
    typeWords = {tuple(wordsOf(typeName)) for typeName in declaredTypeNames(sourceFiles)}
    gluedAccessors = [
        {"file": function.file, "line": function.line, "name": function.name}
        for sourceFile in sourceFiles
        for function in declaredFunctions(sourceFile)
        if isGluedAccessor(function.name, typeWords, nameRules)
    ]
    return {
        "name": "accessorNames",
        "summary": {"gluedAccessorNames": len(gluedAccessors), "measurementIsHeuristic": True},
        "offenders": gluedAccessors[:50],
    }
