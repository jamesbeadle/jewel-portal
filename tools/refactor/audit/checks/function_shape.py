"""Heuristic function-length, branch and else-block measurements for C-family and TypeScript sources."""
from __future__ import annotations

import re

from ..declarations import declaredFunctions
from ..source_files import SourceFile

ELSE_BLOCK = re.compile(r"^\s*}?\s*else\b")
IF_BLOCK = re.compile(r"^\s*(?:}\s*)?(?:else\s+)?(?:@|\{#|\{:else\s+)?if\b")


def countMatchingLines(sourceFiles: list[SourceFile], pattern: re.Pattern) -> int:
    return sum(1 for sourceFile in sourceFiles for line in sourceFile.lines if pattern.match(line))


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    limit = rules["maxFunctionLines"]
    functions = [function for sourceFile in sourceFiles for function in declaredFunctions(sourceFile)]
    longFunctions = [
        {"file": function.file, "lines": function.lines, "name": function.name, "line": function.line}
        for function in functions
        if function.lines > limit
    ]
    longFunctions.sort(key=lambda function: function["lines"], reverse=True)
    return {
        "name": "functionShape",
        "summary": {
            "limit": limit,
            "functionsOverLimit": len(longFunctions),
            "totalFunctions": len(functions),
            "elseBlocks": countMatchingLines(sourceFiles, ELSE_BLOCK),
            "ifBlocks": countMatchingLines(sourceFiles, IF_BLOCK),
            "measurementIsHeuristic": True,
        },
        "offenders": longFunctions[:50],
    }
