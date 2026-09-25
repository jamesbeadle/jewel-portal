"""Which source files a file reaches: what it imports, and for C#, the types it names that another file declares."""
from __future__ import annotations

import re
from collections import defaultdict
from pathlib import PurePosixPath

from ..declarations import TYPE_DECLARATION
from ..source_files import SourceFile

IMPORT = re.compile(r"(?:\bfrom\s+|\bimport\s*\(?\s*|\brequire\(\s*)['\"](?P<module>[^'\"]+)['\"]")
IDENTIFIER = re.compile(r"[A-Za-z_]\w*")
ALIAS_PREFIXES = ("$lib/", "@/", "~/", "./", "../")
TYPE_NAMED_FILES = (".cs", ".razor")
DEPTH = 5


def pathWithoutExtension(relative: str) -> str:
    return str(PurePosixPath(relative).with_suffix(""))


def moduleTail(module: str) -> str:
    tail = pathWithoutExtension(module)
    while tail.startswith(ALIAS_PREFIXES):
        tail = tail.split("/", 1)[1] if "/" in tail else tail
    return tail


class Reach:
    def __init__(self, sourceFiles: list[SourceFile]):
        self.byPath = {sourceFile.relative: sourceFile for sourceFile in sourceFiles}
        self.byTail: dict[str, list[str]] = defaultdict(list)
        self.typeHomes: dict[str, str] = {}
        self.reachedFrom: dict[str, set[str]] = {}
        for sourceFile in sourceFiles:
            self.index(sourceFile)

    def index(self, sourceFile: SourceFile) -> None:
        stem = PurePosixPath(sourceFile.relative)
        self.byTail[stem.stem].append(sourceFile.relative)
        if stem.stem == "index":
            self.byTail[stem.parent.name].append(sourceFile.relative)
        for line in sourceFile.lines:
            declaration = TYPE_DECLARATION.match(line)
            if declaration:
                self.typeHomes.setdefault(declaration.group(1), sourceFile.relative)

    def resolveImport(self, module: str) -> str | None:
        tail = moduleTail(module)
        candidates = self.byTail.get(PurePosixPath(tail).name, [])
        closest = [path for path in candidates if pathWithoutExtension(path).endswith(tail) or path.endswith(f"{tail}/index.ts")]
        chosen = closest or (candidates if len(candidates) == 1 else [])
        return chosen[0] if chosen else None

    def namedTypes(self, sourceFile: SourceFile) -> set[str]:
        names = set(IDENTIFIER.findall("\n".join(sourceFile.lines)))
        implementations = {name[1:] for name in names if name.startswith("I") and name[1:2].isupper()}
        return {self.typeHomes[name] for name in names | implementations if name in self.typeHomes}

    def direct(self, sourceFile: SourceFile) -> set[str]:
        if sourceFile.relative not in self.reachedFrom:
            self.reachedFrom[sourceFile.relative] = self.readReferences(sourceFile)
        return self.reachedFrom[sourceFile.relative]

    def readReferences(self, sourceFile: SourceFile) -> set[str]:
        imported = {self.resolveImport(found.group("module")) for line in sourceFile.lines for found in IMPORT.finditer(line)}
        reached = {path for path in imported if path}
        if sourceFile.relative.endswith(TYPE_NAMED_FILES):
            reached |= self.namedTypes(sourceFile)
        return reached - {sourceFile.relative}

    def closure(self, sourceFile: SourceFile) -> list[SourceFile]:
        seen, frontier = {sourceFile.relative}, [sourceFile]
        for _ in range(DEPTH):
            reached = {path for current in frontier for path in self.direct(current)} - seen
            seen |= reached
            frontier = [self.byPath[path] for path in reached]
        return [self.byPath[path] for path in seen]
