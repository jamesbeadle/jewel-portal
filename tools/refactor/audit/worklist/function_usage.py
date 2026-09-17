"""Where each function is declared and which files use it: the facts behind 'is this the right home?'."""
from __future__ import annotations

import re
from collections import defaultdict

from ..declarations import DeclaredFunction, declaredFunctions
from ..source_files import SourceFile

IDENTIFIER = re.compile(r"[A-Za-z_]\w*")


class FunctionUsage:
    def __init__(self, sourceFiles: list[SourceFile]):
        self.functionsByFile = {sourceFile.relative: declaredFunctions(sourceFile) for sourceFile in sourceFiles}
        declaredNames = {function.name for functions in self.functionsByFile.values() for function in functions if function.name}
        self.filesDeclaring: dict[str, set[str]] = defaultdict(set)
        self.filesMentioning: dict[str, set[str]] = defaultdict(set)
        for file, functions in self.functionsByFile.items():
            for function in functions:
                self.filesDeclaring[function.name].add(file)
        for sourceFile in sourceFiles:
            mentioned = set(IDENTIFIER.findall("\n".join(sourceFile.lines))) & declaredNames
            for name in mentioned:
                self.filesMentioning[name].add(sourceFile.relative)

    def usedByOtherFiles(self, function: DeclaredFunction) -> list[str]:
        return sorted(self.filesMentioning[function.name] - self.filesDeclaring[function.name])

    def alsoDeclaredIn(self, function: DeclaredFunction) -> list[str]:
        return sorted(self.filesDeclaring[function.name] - {function.file})

    def declaredMoreThanOnce(self) -> list[dict]:
        repeated = [
            {"name": name, "declaredIn": sorted(files)}
            for name, files in self.filesDeclaring.items()
            if name and len(files) > 1
        ]
        return sorted(repeated, key=lambda row: len(row["declaredIn"]), reverse=True)


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
