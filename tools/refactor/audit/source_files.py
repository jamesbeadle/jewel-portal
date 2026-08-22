"""Resolves the audited source set from rules.json globs."""
from __future__ import annotations

import fnmatch
import json
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class SourceFile:
    path: Path
    relative: str
    lines: list[str]

    @property
    def lineCount(self) -> int:
        return len(self.lines)


def loadRules(repositoryRoot: Path) -> dict:
    rulesPath = repositoryRoot / "tools" / "refactor" / "rules.json"
    return json.loads(rulesPath.read_text())


def matchesAny(relative: str, globs: list[str]) -> bool:
    return any(fnmatch.fnmatch(relative, pattern) for pattern in globs)


def resolveSourceFiles(repositoryRoot: Path, rules: dict) -> list[SourceFile]:
    resolved: dict[str, SourceFile] = {}
    for pattern in rules["sourceGlobs"]:
        for path in sorted(repositoryRoot.glob(pattern)):
            relative = path.relative_to(repositoryRoot).as_posix()
            if relative in resolved or not path.is_file():
                continue
            if matchesAny(relative, rules["excludeGlobs"]):
                continue
            text = path.read_text(encoding="utf-8", errors="replace")
            resolved[relative] = SourceFile(path, relative, text.splitlines())
    return list(resolved.values())
