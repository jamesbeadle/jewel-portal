"""Flags lines that resist reading as prose: long member chains, deep indentation, overlong lines."""
from __future__ import annotations

import re

from ..source_files import SourceFile

MEMBER_ACCESS = re.compile(r"\w\.\w")
STRING_LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"|@"[^"]*"')
COMMENT_ONLY = re.compile(r"^\s*(//|///|@\*|\*)")
IMPORT_LINE = re.compile(r"^\s*(using|namespace|@using|@namespace|global using)\b")


def stripStringsAndComments(line: str) -> str:
    withoutStrings = STRING_LITERAL.sub('""', line)
    commentStart = withoutStrings.find("//")
    if commentStart >= 0:
        withoutStrings = withoutStrings[:commentStart]
    return withoutStrings


def memberAccessCount(line: str) -> int:
    return len(MEMBER_ACCESS.findall(stripStringsAndComments(line)))


def indentDepth(line: str, indentWidth: int) -> int:
    leadingSpaces = len(line) - len(line.lstrip(" "))
    return leadingSpaces // indentWidth


def isMeasurableCodeLine(line: str) -> bool:
    return bool(line.strip()) and not COMMENT_ONLY.match(line) and not IMPORT_LINE.match(line)


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    proseRules = rules["prose"]
    maxMemberAccesses = proseRules["maxMemberAccessesPerLine"]
    maxLineLength = proseRules["maxLineLength"]
    maxIndentDepth = proseRules["maxIndentDepth"]
    indentWidth = proseRules["indentWidth"]
    longChains = []
    deepLines = []
    overlongLines = []
    for sourceFile in sourceFiles:
        isCSharp = sourceFile.relative.endswith(".cs")
        for lineNumber, line in enumerate(sourceFile.lines, start=1):
            if not isMeasurableCodeLine(line):
                continue
            if memberAccessCount(line) > maxMemberAccesses:
                longChains.append({"file": sourceFile.relative, "line": lineNumber})
            if isCSharp and indentDepth(line, indentWidth) > maxIndentDepth:
                deepLines.append({"file": sourceFile.relative, "line": lineNumber})
            if len(line) > maxLineLength:
                overlongLines.append({"file": sourceFile.relative, "line": lineNumber})
    return {
        "name": "prose",
        "summary": {
            "longMemberChainLines": len(longChains),
            "deeplyIndentedLines": len(deepLines),
            "overlongLines": len(overlongLines),
            "measurementIsHeuristic": True,
        },
        "offenders": {
            "memberChains": longChains[:50],
            "deepIndentation": deepLines[:50],
            "overlongLines": overlongLines[:50],
        },
    }
