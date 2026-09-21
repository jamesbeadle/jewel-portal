"""The files each surface is read from."""
from __future__ import annotations

from pathlib import Path

BUILD_DIRECTORIES = {"obj", "bin"}


def apiFiles(repositoryRoot: Path):
    for path in sorted((repositoryRoot / "api").rglob("*.cs")):
        if not BUILD_DIRECTORIES.intersection(path.parts):
            yield path


def roleVocabularyFiles(repositoryRoot: Path):
    """Where role sets are declared: the shared vocabulary in contracts (read by the API's gates
    and by the pages' OpenTo alike) and the API's own feature-local sets."""
    yield from sorted((repositoryRoot / "contracts/Models").glob("*.cs"))
    yield from apiFiles(repositoryRoot)


def connectorFiles(repositoryRoot: Path):
    return sorted((repositoryRoot / "api/Features/Ai/Tools").rglob("*.cs"))


def viewFiles(repositoryRoot: Path):
    for path in sorted((repositoryRoot / "jpms").rglob("*.razor")):
        if not BUILD_DIRECTORIES.intersection(path.parts):
            yield path
