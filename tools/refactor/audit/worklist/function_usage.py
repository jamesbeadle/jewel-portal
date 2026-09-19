"""Where each function is declared and which files import it: the facts behind 'is this the right home?'.

Two functions are the same function when their bodies are the same, not when their names are: sixteen
components each with their own onSubmit are sixteen functions, and only a body repeated word for word in
several files is one function waiting for a home. Likewise a file uses a function only when it imports it
from the file that declares it; another file merely containing the same word is not a caller.
"""
from __future__ import annotations

import re
from collections import defaultdict

from ..declarations import DeclaredFunction, declaredFunctions
from ..source_files import SourceFile

IDENTIFIER = re.compile(r"[A-Za-z_]\w*")
IMPORT = re.compile(r"import\s+(?:type\s+)?([^;]*?)\s+from\s+['\"]([^'\"]+)['\"]", re.DOTALL)
SOURCE_EXTENSION = re.compile(r"\.(?:svelte|tsx?|jsx?|mjs)$")


def moduleName(path: str) -> str:
    return SOURCE_EXTENSION.sub("", path.rsplit("/", 1)[-1])


def importedNames(sourceFile: SourceFile) -> dict[str, set[str]]:
    imported: dict[str, set[str]] = defaultdict(set)
    for names, modulePath in IMPORT.findall("\n".join(sourceFile.lines)):
        imported[moduleName(modulePath)].update(IDENTIFIER.findall(names))
    return imported


class FunctionUsage:
    def __init__(self, sourceFiles: list[SourceFile]):
        self.functionsByFile = {sourceFile.relative: declaredFunctions(sourceFile) for sourceFile in sourceFiles}
        self.functionsByBody: dict[str, list[DeclaredFunction]] = defaultdict(list)
        self.importsByFile = {sourceFile.relative: importedNames(sourceFile) for sourceFile in sourceFiles}
        for file, functions in self.functionsByFile.items():
            for function in functions:
                if function.bodyFingerprint:
                    self.functionsByBody[function.bodyFingerprint].append(function)

    def importedBy(self, function: DeclaredFunction) -> list[str]:
        module = moduleName(function.file)
        return sorted(
            file for file, imported in self.importsByFile.items()
            if file != function.file and function.name in imported.get(module, set())
        )

    def sameBodyIn(self, function: DeclaredFunction) -> list[str]:
        twins = self.functionsByBody.get(function.bodyFingerprint, [])
        return sorted({twin.file for twin in twins} - {function.file})

    def repeatedBodies(self) -> list[dict]:
        repeated = [
            {
                "name": " / ".join(sorted({function.name for function in functions})),
                "lines": functions[0].lines,
                "declaredIn": sorted({function.file for function in functions}),
            }
            for functions in self.functionsByBody.values()
            if len({function.file for function in functions}) > 1
        ]
        return sorted(repeated, key=lambda row: len(row["declaredIn"]) * row["lines"], reverse=True)


def mentionsName(lines: list[str], name: str) -> bool:
    pattern = re.compile(rf"\b{re.escape(name)}\b")
    return any(pattern.search(line) for line in lines)


def blockOwning(function: DeclaredFunction, lines: list[str], blocks: list[dict]) -> dict | None:
    ownLines = range(function.line - 1, function.line - 1 + function.lines)
    for block in blocks:
        blockLines = range(block["firstLine"] - 1, block["lastLine"])
        inside = [lines[number] for number in blockLines]
        outside = [line for number, line in enumerate(lines) if number not in blockLines and number not in ownLines]
        if mentionsName(inside, function.name) and not mentionsName(outside, function.name):
            return block
    return None
