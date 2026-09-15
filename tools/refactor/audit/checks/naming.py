"""Flags banned abbreviations and unprefixed boolean names."""
from __future__ import annotations

import re

from ..source_files import SourceFile

BOOLEAN_DECLARATION = re.compile(
    r"\bbool\??\s+(\w+)"
    r"|\b(?:const|let|var)\s+(\w+)\s*(?::\s*boolean)?\s*=\s*(?:\$state\()?(?:true|false)\b"
    r"|\b(\w+)\s*:\s*boolean\b"
)


def declaredBooleanName(match: re.Match) -> str:
    return next(group for group in match.groups() if group is not None)


def buildAbbreviationPattern(bannedAbbreviations: list[str]) -> re.Pattern:
    alternatives = "|".join(re.escape(word) for word in bannedAbbreviations)
    return re.compile(rf"\b(?:{alternatives})\b", re.IGNORECASE)


def hasBooleanPrefix(name: str, prefixes: list[str]) -> bool:
    lowered = name.lower()
    return any(lowered.startswith(prefix) for prefix in prefixes)


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    abbreviationPattern = buildAbbreviationPattern(rules["bannedAbbreviations"])
    prefixes = rules["booleanPrefixes"]
    abbreviationHits = []
    unprefixedBooleans = []
    for sourceFile in sourceFiles:
        for lineNumber, line in enumerate(sourceFile.lines, start=1):
            if abbreviationPattern.search(line):
                abbreviationHits.append({"file": sourceFile.relative, "line": lineNumber})
            for match in BOOLEAN_DECLARATION.finditer(line):
                name = declaredBooleanName(match)
                if not hasBooleanPrefix(name, prefixes):
                    unprefixedBooleans.append(
                        {"file": sourceFile.relative, "line": lineNumber, "name": name}
                    )
    return {
        "name": "naming",
        "summary": {
            "bannedAbbreviationHits": len(abbreviationHits),
            "unprefixedBooleans": len(unprefixedBooleans),
        },
        "offenders": {
            "abbreviations": abbreviationHits[:50],
            "booleans": unprefixedBooleans[:50],
        },
    }
