"""Finds functions nothing calls: a name that appears once in the whole source is only its own declaration."""
from __future__ import annotations

import re
from collections import Counter

from ..declarations import DeclaredFunction, declaredFunctions
from ..source_files import SourceFile, matchesAny

IDENTIFIER = re.compile(r"[A-Za-z_]\w*")
ENTRY_POINTS_WHEN_UNSET = [
    "main", "Main", "load", "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD", "fallback",
    "handle", "handleError", "handleFetch", "reroute", "init", "entries",
    "Configure", "ConfigureServices", "Dispose", "DisposeAsync", "BuildRenderTree", "ShouldRender",
    "OnInitialized", "OnInitializedAsync", "OnParametersSet", "OnParametersSetAsync",
    "OnAfterRender", "OnAfterRenderAsync", "Handle", "Invoke", "InvokeAsync", "Execute", "ExecuteAsync",
    "ToString", "Equals", "GetHashCode", "Up", "Down",
]
EXEMPT_GLOBS_WHEN_UNSET = ["**/*.test.*", "**/*.spec.*", "**/tests/**", "**/Tests/**", "**/*Tests.cs"]


def identifierCounts(sourceFiles: list[SourceFile]) -> Counter[str]:
    counts: Counter[str] = Counter()
    for sourceFile in sourceFiles:
        for line in sourceFile.lines:
            counts.update(IDENTIFIER.findall(line))
    return counts


def isCalledByTheFramework(function: DeclaredFunction, orphanRules: dict) -> bool:
    if function.isAttributed:
        return True
    if function.name in orphanRules.get("entryPointNames", ENTRY_POINTS_WHEN_UNSET):
        return True
    return matchesAny(function.file, orphanRules.get("exemptGlobs", EXEMPT_GLOBS_WHEN_UNSET))


def check(sourceFiles: list[SourceFile], rules: dict) -> dict:
    orphanRules = rules.get("orphans", {})
    counts = identifierCounts(sourceFiles)
    functions = [function for sourceFile in sourceFiles for function in declaredFunctions(sourceFile)]
    orphanFunctions = [
        {"file": function.file, "line": function.line, "name": function.name}
        for function in functions
        if function.name and counts[function.name] == 1 and not isCalledByTheFramework(function, orphanRules)
    ]
    return {
        "name": "orphans",
        "summary": {"orphanFunctions": len(orphanFunctions), "functionsExamined": len(functions)},
        "offenders": orphanFunctions[:100],
    }
