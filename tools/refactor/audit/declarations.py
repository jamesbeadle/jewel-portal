"""The functions and types a source file declares: the shared reading every name-based check starts from."""
from __future__ import annotations

import hashlib
import re
from dataclasses import dataclass

from .signatures import isFunctionSignature
from .source_files import SourceFile

TYPE_DECLARATION = re.compile(
    r"^\s*(?:export\s+)?(?:declare\s+)?(?:default\s+)?"
    r"(?:public|private|protected|internal|sealed|static|abstract|partial|\s)*"
    r"\b(?:class|record|struct|interface|enum|type)\s+(\w+)"
)
NAME_BEFORE_PARENTHESIS = re.compile(r"(\w+)\s*(?:<[^>()]*>)?\s*\(")
NAME_BEFORE_ARROW = re.compile(r"\b(?:const|let)\s+(\w+)")
ATTRIBUTE_LINE = re.compile(r"^\s*(?:\[[\w\.]+.*\]|@\w[\w\.]*(?:\(.*\))?)\s*$")
SMALLEST_COMPARABLE_BODY = 3
KEYWORDS_MISTAKEN_FOR_NAMES = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "function"}


@dataclass(frozen=True)
class DeclaredFunction:
    file: str
    name: str
    line: int
    lines: int
    isAttributed: bool
    bodyFingerprint: str


def nameOnSignature(line: str) -> str:
    arrowName = NAME_BEFORE_ARROW.search(line)
    if arrowName and "=>" in line:
        return arrowName.group(1)
    candidates = [name for name in NAME_BEFORE_PARENTHESIS.findall(line) if name not in KEYWORDS_MISTAKEN_FOR_NAMES]
    return candidates[0] if candidates else ""


def previousLineIsAttribute(lines: list[str], lineNumber: int) -> bool:
    earlierLines = [line for line in lines[:lineNumber] if line.strip()]
    return bool(earlierLines) and bool(ATTRIBUTE_LINE.match(earlierLines[-1]))


def fingerprintOf(bodyLines: list[str]) -> str:
    statements = ["".join(line.split()) for line in bodyLines if line.strip()]
    if len(statements) < SMALLEST_COMPARABLE_BODY:
        return ""
    return hashlib.sha1("\n".join(statements).encode()).hexdigest()


def declaredFunctions(sourceFile: SourceFile) -> list[DeclaredFunction]:
    functions = []
    depthAtFunctionStart = None
    depth = 0
    startLine = 0
    for lineNumber, line in enumerate(sourceFile.lines):
        if depthAtFunctionStart is None and isFunctionSignature(line):
            depthAtFunctionStart = depth
            startLine = lineNumber
        depth += line.count("{") - line.count("}")
        isFunctionClosed = depthAtFunctionStart is not None and depth <= depthAtFunctionStart and "}" in line
        if not isFunctionClosed:
            continue
        functions.append(DeclaredFunction(
            file=sourceFile.relative,
            name=nameOnSignature(sourceFile.lines[startLine]),
            line=startLine + 1,
            lines=lineNumber - startLine + 1,
            isAttributed=previousLineIsAttribute(sourceFile.lines, startLine),
            bodyFingerprint=fingerprintOf(sourceFile.lines[startLine + 1:lineNumber]),
        ))
        depthAtFunctionStart = None
    return functions


def declaredTypeNames(sourceFiles: list[SourceFile]) -> set[str]:
    return {
        match.group(1)
        for sourceFile in sourceFiles
        for line in sourceFile.lines
        if (match := TYPE_DECLARATION.match(line))
    }
