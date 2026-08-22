"""Flags files longer than the hard limit in rules.json."""
from __future__ import annotations

from ..source_files import SourceFile, matchesAny


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    limit = rules["maxFileLines"]
    exemptGlobs = rules.get("fileLengthExemptGlobs", [])
    offenders = [
        {"file": sourceFile.relative, "lines": sourceFile.lineCount}
        for sourceFile in sourceFiles
        if sourceFile.lineCount > limit
        and not matchesAny(sourceFile.relative, exemptGlobs)
    ]
    offenders.sort(key=lambda offender: offender["lines"], reverse=True)
    return {
        "name": "fileLength",
        "summary": {
            "limit": limit,
            "filesOverLimit": len(offenders),
            "totalFiles": len(sourceFiles),
            "worstFileLines": offenders[0]["lines"] if offenders else 0,
        },
        "offenders": offenders,
    }
