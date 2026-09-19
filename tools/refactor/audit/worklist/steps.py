"""Orders the refactor for this repository: component breakout, then utility functions, then design patterns."""
from __future__ import annotations


BREAKOUT, UTILITIES, PATTERNS = "Pass 2 — Component breakout", "Pass 3 — Utility function identification", "Pass 4 — Design pattern identification"
SHOWN_BLOCKS = 4
SHOWN_FILES = 4
SHOWN_REPEATS = 15


def step(passName: str, title: str, detail: str) -> dict:
    return {"pass": passName, "title": title, "detail": detail}


def describeBlock(block: dict, target: dict) -> str:
    movers = [function["name"] for function in target["functions"] if function["movesWithBlockAtLine"] == block["firstLine"]]
    taking = f", taking {', '.join(movers)}" if movers else ""
    return f"lines {block['firstLine']}–{block['lastLine']} ({block['lines']} lines{taking})"


def breakoutSteps(targets: list[dict]) -> list[dict]:
    steps = []
    for target in targets:
        blocks = "; ".join(describeBlock(block, target) for block in target["blocks"][:SHOWN_BLOCKS])
        detail = f"Component-sized blocks: {blocks}." if blocks else "No block was large enough to measure: read the view for its seams."
        steps.append(step(BREAKOUT, f"Break `{target['file']}` ({target['lines']} lines) into components", detail))
    return steps


def sharedFromViewSteps(targets: list[dict]) -> list[dict]:
    return [
        step(UTILITIES, f"Give `{function['name']}` a home of its own, out of `{target['file']}`",
             f"{len(function['importedBy'])} other files import it from there: {', '.join(function['importedBy'][:SHOWN_FILES])}.")
        for target in targets
        for function in target["functions"]
        if function["importedBy"]
    ]


def utilitySteps(views: list[dict], repeatedBodies: list[dict], audit: dict) -> list[dict]:
    steps = [
        step(UTILITIES, f"Give `{row['name']}` one home",
             f"The same {row['lines']}-line function, word for word, is in {len(row['declaredIn'])} files: {', '.join(row['declaredIn'][:SHOWN_FILES])}.")
        for row in repeatedBodies[:SHOWN_REPEATS]
    ]
    steps += sharedFromViewSteps(views)
    orphanCount = audit["summaries"].get("orphans", {}).get("orphanFunctions", 0) + audit["summaries"].get("inventory", {}).get("orphanComponents", 0)
    if orphanCount:
        steps.append(step(UTILITIES, f"Remove the {orphanCount} components and functions nothing calls",
                          "Listed in audit.json under details.orphans and details.inventory.offenders.orphans; confirm each has no caller before it goes."))
    return steps


def describeGaps(pattern: dict) -> str:
    predicted = "; ".join(pattern["missing"][:SHOWN_FILES])
    return (
        f"{len(pattern['missing'])} of {pattern['predicted']} lack it. Predicted: {predicted}. "
        "Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps."
    )


def patternSteps(others: list[dict], designPatterns: dict) -> list[dict]:
    offenders = designPatterns["offenders"]
    steps = [
        step(PATTERNS, f"Complete the pattern: {pattern['pattern']}", describeGaps(pattern))
        for pattern in offenders["patterns"]
        if pattern["missing"]
    ]
    steps += [
        step(PATTERNS, f"Bring `{row['entity']}` into range",
             f"{row['properties']} properties, {row['files']} file(s) named for it, about {row['expectedFiles']} expected.")
        for row in offenders["entityFileCounts"]
        if not row["isInRange"]
    ]
    steps += [
        step(PATTERNS, f"Divide `{target['file']}` ({target['lines']} lines) into the units its pattern names",
             f"{len(target['functions'])} functions; the longest is {max((function['lines'] for function in target['functions']), default=0)} lines.")
        for target in others
    ]
    return steps
