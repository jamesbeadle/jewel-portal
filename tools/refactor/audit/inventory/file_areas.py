"""Counts every file in the repository by the area it belongs to: frontend, API, backend, database, infrastructure…

The areas are an ordered list in rules.json; a file belongs to the first area whose globs it matches, so the
narrow areas (tests, API) come before the broad ones (backend, frontend). What matches nothing is "other".
"""
from __future__ import annotations

import subprocess
from pathlib import Path

from ..source_files import SourceFile, matchesAny

OTHER = "other"
IGNORED_WHEN_NOT_A_GIT_REPOSITORY = [
    "**/.git/**", "**/node_modules/**", "**/bin/**", "**/obj/**", "**/.svelte-kit/**", "**/dist/**", "**/build/**",
]
AREAS_WHEN_UNSET = [
    {"name": "tests", "globs": ["**/*.test.*", "**/*.spec.*", "**/tests/**", "**/Tests/**", "**/*.Tests/**", "**/*Tests.cs"]},
    {"name": "docs", "globs": ["**/*.md", "docs/**"]},
    {"name": "tooling", "globs": ["tools/**", ".claude/**", "scripts/**"]},
    {"name": "database", "globs": ["**/migrations/**", "**/Migrations/**", "**/*.sql"]},
    {"name": "api", "globs": [
        "**/api/**", "**/Api/**", "**/controllers/**", "**/Controllers/**",
        "**/endpoints/**", "**/Endpoints/**", "**/+server.*", "**/mcp/**",
    ]},
    {"name": "frontend", "globs": [
        "**/*.tsx", "**/*.jsx", "**/*.vue", "**/*.svelte", "**/*.html",
        "**/*.css", "**/*.scss", "**/components/**", "public/**", "static/**",
        "**/*.razor", "**/*.razor.cs", "**/wwwroot/**",
    ]},
    {"name": "backend", "globs": ["**/*.ts", "**/*.js", "**/*.py", "**/*.cs", "**/*.go", "**/*.java", "**/*.kt", "**/*.swift", "**/*.rb", "**/*.php"]},
    {"name": "infrastructure", "globs": [".github/**", "infra/**", "**/*.tf", "**/*.bicep", "**/Dockerfile*", "*.json", "*.yml", "*.yaml", "*.toml", ".*"]},
]


def repositoryFiles(repositoryRoot: Path) -> list[str]:
    listing = subprocess.run(["git", "ls-files"], cwd=repositoryRoot, capture_output=True, text=True)
    if listing.returncode == 0 and listing.stdout.strip():
        return listing.stdout.splitlines()
    walked = [path.relative_to(repositoryRoot).as_posix() for path in repositoryRoot.rglob("*") if path.is_file()]
    return [relative for relative in walked if not matchesAny(relative, IGNORED_WHEN_NOT_A_GIT_REPOSITORY)]


def areaOf(relative: str, areas: list[dict]) -> str:
    return next((area["name"] for area in areas if matchesAny(relative, area["globs"])), OTHER)


def check(repositoryRoot: Path, sourceFiles: list[SourceFile], rules: dict) -> dict:
    areas = rules.get("areas", AREAS_WHEN_UNSET)
    areaNames = [area["name"] for area in areas] + [OTHER]
    rows = {name: {"area": name, "files": 0, "sourceFiles": 0, "sourceLines": 0} for name in areaNames}
    allFiles = repositoryFiles(repositoryRoot)
    for relative in allFiles:
        rows[areaOf(relative, areas)]["files"] += 1
    for sourceFile in sourceFiles:
        row = rows[areaOf(sourceFile.relative, areas)]
        row["sourceFiles"] += 1
        row["sourceLines"] += sourceFile.lineCount
    populated = sorted((row for row in rows.values() if row["files"]), key=lambda row: row["files"], reverse=True)
    return {
        "name": "fileAreas",
        "summary": {"totalFiles": len(allFiles), **{row["area"]: row["files"] for row in populated}},
        "offenders": populated,
    }


def areasLine(fileAreas: dict) -> str:
    parts = [f"{row['area']} {row['files']:,}" for row in fileAreas["offenders"]]
    return f"{fileAreas['summary']['totalFiles']:,} files · " + " · ".join(parts)


def areasTable(fileAreas: dict) -> str:
    rows = ["| Area | Files | Of which audited source | Source lines |", "| --- | --- | --- | --- |"]
    rows += [f"| {row['area']} | {row['files']:,} | {row['sourceFiles']:,} | {row['sourceLines']:,} |" for row in fileAreas["offenders"]]
    total = fileAreas["summary"]["totalFiles"]
    sourceFiles = sum(row["sourceFiles"] for row in fileAreas["offenders"])
    sourceLines = sum(row["sourceLines"] for row in fileAreas["offenders"])
    rows.append(f"| **whole repository** | **{total:,}** | **{sourceFiles:,}** | **{sourceLines:,}** |")
    return "\n".join(rows)
