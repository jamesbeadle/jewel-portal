"""The files each surface is read from."""
from __future__ import annotations

from pathlib import Path

BUILD_DIRECTORIES = {"obj", "bin"}


def apiFiles(repositoryRoot: Path):
    for path in sorted((repositoryRoot / "api").rglob("*.cs")):
        if not BUILD_DIRECTORIES.intersection(path.parts):
            yield path


def connectorFiles(repositoryRoot: Path):
    return sorted((repositoryRoot / "api/Features/Ai/Tools").rglob("*.cs"))


def viewFiles(repositoryRoot: Path):
    for path in sorted((repositoryRoot / "jpms").rglob("*.razor")):
        if not BUILD_DIRECTORIES.intersection(path.parts):
            yield path
