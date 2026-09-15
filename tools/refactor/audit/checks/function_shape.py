"""Heuristic function-length and else-block measurements for C-family and TypeScript sources."""
from __future__ import annotations

import re

from ..source_files import SourceFile

C_FAMILY_SIGNATURE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|async|override|sealed|partial|virtual)"
    r"[\w\s<>,\[\]\?]*\s+\w+\s*\([^;]*$|^\s*(?:public|private|protected|internal)"
    r"[\w\s<>,\[\]\?]*\s+\w+\s*\([^)]*\)\s*$"
)
TYPESCRIPT_SIGNATURE = re.compile(
    r"^\s*(?:export\s+)?(?:default\s+)?(?:async\s+)?function\s*\*?\s*\w*\s*(?:<[^>]*>)?\s*\("
    r"|^\s*(?:export\s+)?(?:const|let)\s+\w+\s*(?::[^=]+)?=\s*(?:async\s*)?(?:\([^)]*\)|\w+)\s*(?::\s*[^=]+)?=>\s*\{\s*$"
)
ELSE_BLOCK = re.compile(r"^\s*}?\s*else\b")


def isFunctionSignature(line: str) -> bool:
    return bool(C_FAMILY_SIGNATURE.match(line) or TYPESCRIPT_SIGNATURE.match(line))


def measureFunctionLengths(lines: list[str]) -> list[int]:
    lengths = []
    depthAtFunctionStart = None
    depth = 0
    startLine = 0
    for lineNumber, line in enumerate(lines):
        if depthAtFunctionStart is None and isFunctionSignature(line):
            depthAtFunctionStart = depth
            startLine = lineNumber
        depth += line.count("{") - line.count("}")
        isFunctionClosed = depthAtFunctionStart is not None and depth <= depthAtFunctionStart and "}" in line
        if isFunctionClosed:
            lengths.append(lineNumber - startLine + 1)
            depthAtFunctionStart = None
    return lengths


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    limit = rules["maxFunctionLines"]
    longFunctions = []
    elseCount = 0
    for sourceFile in sourceFiles:
        elseCount += sum(1 for line in sourceFile.lines if ELSE_BLOCK.match(line))
        for length in measureFunctionLengths(sourceFile.lines):
            if length > limit:
                longFunctions.append({"file": sourceFile.relative, "lines": length})
    longFunctions.sort(key=lambda function: function["lines"], reverse=True)
    return {
        "name": "functionShape",
        "summary": {
            "limit": limit,
            "functionsOverLimit": len(longFunctions),
            "elseBlocks": elseCount,
            "measurementIsHeuristic": True,
        },
        "offenders": longFunctions[:50],
    }
