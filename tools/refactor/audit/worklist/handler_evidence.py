"""What the code itself says about who calls a function, without knowing any one framework's vocabulary.

A handler the framework owns is bound from its own file's markup - on:submit={onSubmit}, @onclick="OnSave",
onSubmit={handleSubmit}, (submit)="onSubmit()" - or is invoked directly through an attribute or an override.
A template kept beside its code - a razor page next to its code-behind, a component's html next
to its ts - counts as that file's own markup. A shared utility is imported by another file. Those are shapes every framework repeats, so a framework nobody
has listed still reads correctly. What the shapes cannot settle is returned as undecided, for the round to
judge and record once in frameworkHandlers.names.
"""
from __future__ import annotations

import re

BOUND_TO_ATTRIBUTE = r"[\w:@()\[\].-]+\s*=\s*[\"'{]\s*(?:@?\(\s*\)\s*=>\s*)?@?"
OVERRIDE = re.compile(r"\boverride\b")
NAME_PATTERNS_WHEN_UNSET = [r"^on[A-Z]", r"^handle[A-Z]", r"^On[A-Z]", r"^Handle[A-Z]"]
COINCIDENCE_LINES_WHEN_UNSET = 12
LOCAL, SHARED, UNDECIDED = "componentLocal", "shared", "undecided"


def companionKey(path: str) -> str:
    directory, _, filename = path.rpartition("/")
    return f"{directory}/{filename.split('.')[0]}"


def isBoundInMarkup(name: str, lines: list) -> bool:
    binding = re.compile(BOUND_TO_ATTRIBUTE + re.escape(name) + r"\b")
    return any(binding.search(line) for line in lines)


def isInvokedByFramework(isAttributed: bool, signatureLine: str) -> bool:
    return isAttributed or bool(OVERRIDE.search(signatureLine))


def matchesHandlerShape(name: str, handlerRules: dict) -> bool:
    patterns = handlerRules.get("namePatterns", NAME_PATTERNS_WHEN_UNSET)
    return any(re.search(pattern, name) for pattern in patterns)


def isSettledAsHandler(name: str, handlerRules: dict) -> bool:
    return name in handlerRules.get("names", [])


def isShortEnoughToBeCoincidence(lines: int, handlerRules: dict) -> bool:
    return lines <= handlerRules.get("coincidenceLines", COINCIDENCE_LINES_WHEN_UNSET)


def evidenceFor(name: str, isAttributed: bool, lines: list, signature: str, isImported: bool, handlerRules: dict) -> dict:
    return {
        "isBoundInMarkup": isBoundInMarkup(name, lines),
        "isInvokedByFramework": isInvokedByFramework(isAttributed, signature),
        "isImportedElsewhere": isImported,
        "matchesHandlerShape": matchesHandlerShape(name, handlerRules),
        "isSettledAsHandler": isSettledAsHandler(name, handlerRules),
    }


def looksFrameworkOwned(evidence: dict) -> bool:
    return evidence["isBoundInMarkup"] or evidence["isInvokedByFramework"]


def verdictOn(evidences: list) -> str:
    if all(evidence["isSettledAsHandler"] for evidence in evidences):
        return LOCAL
    if any(evidence["isImportedElsewhere"] for evidence in evidences):
        return SHARED
    if all(looksFrameworkOwned(evidence) for evidence in evidences):
        return LOCAL
    if all(evidence["matchesHandlerShape"] for evidence in evidences):
        return LOCAL
    if any(looksFrameworkOwned(evidence) or evidence["matchesHandlerShape"] for evidence in evidences):
        return UNDECIDED
    return SHARED


def reasonFor(verdict: str, evidences: list) -> str:
    bound = sum(1 for evidence in evidences if evidence["isBoundInMarkup"])
    invoked = sum(1 for evidence in evidences if evidence["isInvokedByFramework"])
    imported = sum(1 for evidence in evidences if evidence["isImportedElsewhere"])
    total = len(evidences)
    return f"{bound}/{total} bound from their own markup, {invoked}/{total} framework-invoked, {imported}/{total} imported elsewhere"
