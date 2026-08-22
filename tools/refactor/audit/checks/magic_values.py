"""Flags inline hex colours, inline style attributes, and repeated string literals."""
from __future__ import annotations

import re
from collections import Counter

from ..source_files import SourceFile, matchesAny

HEX_COLOUR = re.compile(r"#[0-9a-fA-F]{6}\b")
INLINE_STYLE = re.compile(r'style="')
STRING_LITERAL = re.compile(r'"([A-Za-z][A-Za-z0-9 _\-/]{5,40})"')
REPEATED_LITERAL_THRESHOLD = 4


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    markupGlobs = rules["styleTokens"]["markupGlobs"]
    hexColourHits = []
    inlineStyleHits = []
    literalCounts: Counter[str] = Counter()
    for sourceFile in sourceFiles:
        isMarkup = matchesAny(sourceFile.relative, markupGlobs)
        for lineNumber, line in enumerate(sourceFile.lines, start=1):
            if isMarkup and HEX_COLOUR.search(line):
                hexColourHits.append({"file": sourceFile.relative, "line": lineNumber})
            if isMarkup and INLINE_STYLE.search(line):
                inlineStyleHits.append({"file": sourceFile.relative, "line": lineNumber})
            for match in STRING_LITERAL.finditer(line):
                literalCounts[match.group(1)] += 1
    repeatedLiterals = [
        {"literal": literal, "occurrences": count}
        for literal, count in literalCounts.most_common(30)
        if count >= REPEATED_LITERAL_THRESHOLD
    ]
    return {
        "name": "magicValues",
        "summary": {
            "inlineHexColours": len(hexColourHits),
            "inlineStyleAttributes": len(inlineStyleHits),
            "repeatedStringLiterals": len(repeatedLiterals),
        },
        "offenders": {
            "hexColours": hexColourHits[:50],
            "inlineStyles": inlineStyleHits[:50],
            "repeatedLiterals": repeatedLiterals,
        },
    }
