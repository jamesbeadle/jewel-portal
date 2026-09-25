"""Chooses the round's target files, worst first, and sorts them into views and everything else."""
from __future__ import annotations

from ..checks import file_length
from ..source_files import SourceFile, matchesAny
from .block_ownership import blockOwning
from .function_usage import FunctionUsage
from .markup_blocks import componentCandidates

STYLESHEET_SUFFIX = ".css"
MINIMUM_BLOCK_LINES = 12
MAXIMUM_BLOCK_LINES = 80


def isView(sourceFile: SourceFile, rules: dict) -> bool:
    markupGlobs = rules.get("styleTokens", {}).get("markupGlobs", [])
    return matchesAny(sourceFile.relative, markupGlobs) and not sourceFile.relative.endswith(STYLESHEET_SUFFIX)


def worstFiles(sourceFiles: list[SourceFile], rules: dict) -> list[SourceFile]:
    byPath = {sourceFile.relative: sourceFile for sourceFile in sourceFiles}
    offenders = file_length.check(sourceFiles, rules)["offenders"]
    return [byPath[offender["file"]] for offender in offenders]


def functionRows(sourceFile: SourceFile, usage: FunctionUsage, blocks: list[dict]) -> list[dict]:
    rows = []
    for function in usage.functionsByFile[sourceFile.relative]:
        owner = blockOwning(function, sourceFile.lines, blocks)
        rows.append({
            "name": function.name, "line": function.line, "lines": function.lines,
            "importedBy": usage.importedBy(function),
            "sameBodyIn": usage.sameBodyIn(function),
            "movesWithBlockAtLine": owner["firstLine"] if owner else None,
        })
    return rows


def describeTarget(sourceFile: SourceFile, rules: dict, usage: FunctionUsage) -> dict:
    worklistRules = rules.get("worklist", {})
    blocks = []
    if isView(sourceFile, rules):
        blocks = componentCandidates(
            sourceFile.lines,
            worklistRules.get("minimumBlockLines", MINIMUM_BLOCK_LINES),
            worklistRules.get("maximumBlockLines", MAXIMUM_BLOCK_LINES),
        )
    return {
        "file": sourceFile.relative, "lines": sourceFile.lineCount, "isView": isView(sourceFile, rules),
        "blocks": blocks, "functions": functionRows(sourceFile, usage, blocks),
    }
