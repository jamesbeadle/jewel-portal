"""Flags function names so long they signal a missing type: the name wants to be Class.method."""
from __future__ import annotations

import re

from ..source_files import SourceFile

METHOD_DECLARATION = re.compile(
    r"^\s*(?:public|private|protected|internal)\b"
    r"[\w\s<>,\[\]\?]*?\s+(\w+)\s*(?:<[\w\s,]+>)?\s*\("
)
KEYWORDS_MISTAKEN_FOR_NAMES = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "get", "set"}


def wordCount(name: str) -> int:
    return len(re.findall(r"[A-Z][a-z0-9]*|^[a-z0-9]+", name))


def isOverlong(name: str, rules: dict) -> bool:
    return wordCount(name) > rules["maxWords"] or len(name) > rules["maxLength"]


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    nameRules = rules["functionNames"]
    overlongNames = []
    for sourceFile in sourceFiles:
        for lineNumber, line in enumerate(sourceFile.lines, start=1):
            match = METHOD_DECLARATION.match(line)
            if not match:
                continue
            name = match.group(1)
            if name in KEYWORDS_MISTAKEN_FOR_NAMES:
                continue
            if isOverlong(name, nameRules):
                overlongNames.append(
                    {"file": sourceFile.relative, "line": lineNumber, "name": name}
                )
    return {
        "name": "functionNames",
        "summary": {
            "overlongFunctionNames": len(overlongNames),
            "maxWords": nameRules["maxWords"],
            "maxLength": nameRules["maxLength"],
        },
        "offenders": overlongNames[:50],
    }
