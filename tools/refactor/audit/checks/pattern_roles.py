"""Reads the design patterns off the file names and predicts the files each pattern says should exist.

A file name is a subject and a role: CreateProjectHandler is the Handler of CreateProject. A role many
subjects share is a family; when nearly every subject with one role also has another, the pattern predicts
that file for the subjects still missing it.
"""
from __future__ import annotations

from collections import defaultdict
from pathlib import PurePosixPath

from ..source_files import SourceFile
from ..words import wordsOf

MINIMUM_FAMILY_SIZE = 4
COOCCURRENCE_THRESHOLD = 0.75


def subjectAndRole(relative: str) -> tuple[str, str] | None:
    path = PurePosixPath(relative)
    if path.name.startswith("+"):
        return path.parent.as_posix(), path.name
    words = wordsOf(path.name.rsplit(".", 1)[0])
    if len(words) < 2:
        return None
    return "-".join(words[:-1]), words[-1]


def filesByRole(sourceFiles: list[SourceFile], minimumFamilySize: int) -> dict[str, dict[str, str]]:
    files: dict[str, dict[str, str]] = defaultdict(dict)
    for sourceFile in sourceFiles:
        named = subjectAndRole(sourceFile.relative)
        if named:
            files[named[1]][named[0]] = sourceFile.relative
    return {role: members for role, members in files.items() if len(members) >= minimumFamilySize}


def predictedPath(existingPath: str, role: str, otherRole: str) -> str:
    path = PurePosixPath(existingPath)
    if path.name.startswith("+"):
        return (path.parent / otherRole).as_posix()
    stem, _, extension = path.name.rpartition(".")
    position = stem.lower().rfind(role)
    isCapitalised = stem[position].isupper()
    renamed = stem[:position] + (otherRole.capitalize() if isCapitalised else otherRole)
    return (path.parent / f"{renamed}.{extension}").as_posix()


def predictionsBetween(role: str, members: dict[str, str], otherRole: str, otherMembers: dict[str, str], threshold: float) -> dict | None:
    confidence = len(members.keys() & otherMembers.keys()) / len(members)
    if role == otherRole or confidence < threshold:
        return None
    lacking = sorted(members.keys() - otherMembers.keys())
    return {
        "pattern": f"every {role} has a {otherRole}",
        "predicted": len(members),
        "missing": [predictedPath(members[subject], role, otherRole) for subject in lacking],
    }


def measure(sourceFiles: list[SourceFile], patternRules: dict) -> dict:
    families = filesByRole(sourceFiles, patternRules.get("minimumFamilySize", MINIMUM_FAMILY_SIZE))
    threshold = patternRules.get("cooccurrenceThreshold", COOCCURRENCE_THRESHOLD)
    acceptedGaps = set(patternRules.get("acceptedGaps", []))
    patterns = [
        prediction
        for role, members in families.items()
        for otherRole, otherMembers in families.items()
        if (prediction := predictionsBetween(role, members, otherRole, otherMembers, threshold))
    ]
    missing = [gap for pattern in patterns for gap in pattern["missing"] if gap not in acceptedGaps]
    return {
        "roleFamilies": {role: len(members) for role, members in sorted(families.items())},
        "patterns": patterns,
        "predictedFiles": sum(pattern["predicted"] for pattern in patterns),
        "predictedFilesMissing": len(missing),
        "missing": missing,
    }
