"""Where each function is declared and which files import it: the facts behind 'is this the right home?'.

Two functions are the same function when their bodies are the same, not when their names are: sixteen
components each with their own onSubmit are sixteen functions, and only a body repeated word for word in
several files is one function waiting for a home. Whether a name is the framework's is read from the code, not
from a list of framework vocabulary: a handler is bound from its own file's markup or invoked through an
attribute or override, while a utility is imported by another file. A short body that reads as the
framework's is dropped, a long one is reported as a body to extract rather than a function to move, and
evidence that does not settle it is returned undecided for the round to judge. Likewise a file
uses a function only when it imports it from the file that declares it; another file merely containing the
same word is not a caller.
"""
from __future__ import annotations

import re
from collections import defaultdict

from ..declarations import DeclaredFunction, declaredFunctions
from ..source_files import SourceFile
from .handler_evidence import (LOCAL, companionKey, evidenceFor, isShortEnoughToBeCoincidence,
                               reasonFor, verdictOn)

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
        self.linesByFile = {sourceFile.relative: sourceFile.lines for sourceFile in sourceFiles}
        self.linesByCompanion: dict[str, list] = defaultdict(list)
        for sourceFile in sourceFiles:
            self.linesByCompanion[companionKey(sourceFile.relative)].extend(sourceFile.lines)
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

    def evidenceOn(self, function: DeclaredFunction, handlerRules: dict) -> dict:
        lines = self.linesByFile[function.file]
        signature = lines[function.line - 1] if function.line <= len(lines) else ""
        markup = self.linesByCompanion[companionKey(function.file)]
        isImported = bool(self.importedBy(function))
        return evidenceFor(function.name, function.isAttributed, markup, signature, isImported, handlerRules)

    def repeatedBodies(self, handlerRules: dict) -> list[dict]:
        repeated = []
        for functions in self.functionsByBody.values():
            files = sorted({function.file for function in functions})
            if len(files) < 2:
                continue
            lines = functions[0].lines
            evidences = [self.evidenceOn(function, handlerRules) for function in functions]
            verdict = verdictOn(evidences)
            if verdict == LOCAL and isShortEnoughToBeCoincidence(lines, handlerRules):
                continue
            repeated.append({
                "name": " / ".join(sorted({function.name for function in functions})),
                "lines": lines,
                "declaredIn": files,
                "verdict": verdict,
                "evidence": reasonFor(verdict, evidences),
            })
        return sorted(repeated, key=lambda row: len(row["declaredIn"]) * row["lines"], reverse=True)

