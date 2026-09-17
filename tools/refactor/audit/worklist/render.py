"""Writes the refactoring plan as markdown: the order first, then the measured detail behind the first targets."""
from __future__ import annotations

SHOWN_FILES = 4
DETAILED_TARGETS = 10


def shortList(files: list[str]) -> str:
    if not files:
        return "—"
    extra = f" +{len(files) - SHOWN_FILES}" if len(files) > SHOWN_FILES else ""
    return ", ".join(f"`{file}`" for file in files[:SHOWN_FILES]) + extra


def orderedSteps(steps: list[dict], firstNumber: int = 1) -> str:
    lines = []
    currentPass = None
    for number, step in enumerate(steps, start=firstNumber):
        if step["pass"] != currentPass:
            currentPass = step["pass"]
            lines += ["", f"**{currentPass}**", ""]
        lines.append(f"{number}. {step['title']}. {step['detail']}")
    return "\n".join(lines).strip()


def blockTable(target: dict) -> str:
    rows = ["| Block opens with | Lines | Range | Functions that move with it |", "| --- | --- | --- | --- |"]
    for block in target["blocks"]:
        movers = [function["name"] for function in target["functions"] if function["movesWithBlockAtLine"] == block["firstLine"]]
        opens = block["opens"].replace("|", "\\|")
        rows.append(f"| `{opens}` | {block['lines']} | {block['firstLine']}–{block['lastLine']} | {', '.join(movers) or '—'} |")
    return "\n".join(rows)


def functionTable(target: dict) -> str:
    rows = ["| Function | Line | Lines | Used by other files | Also declared in |", "| --- | --- | --- | --- | --- |"]
    rows += [
        f"| {function['name']} | {function['line']} | {function['lines']} | "
        f"{shortList(function['usedByOtherFiles'])} | {shortList(function['alsoDeclaredIn'])} |"
        for function in target["functions"]
    ]
    return "\n".join(rows)


def targetSection(target: dict) -> str:
    parts = [f"### `{target['file']}` — {target['lines']} lines"]
    if target["blocks"]:
        parts.append(blockTable(target))
    if target["functions"]:
        parts.append(functionTable(target))
    return "\n\n".join(parts)


def renderPlan(plan: dict) -> str:
    return "\n\n".join([
        "# Refactoring plan",
        f"Written by the code quality check at a score of {plan['score']}%. These are the steps a refactor of this "
        "repository follows, in this order; a round takes the next steps from the top. The plan is measured, so a "
        "finished step is gone the next time the check runs. The facts are measured; the judgement is the round's.",
        "## The order",
        orderedSteps(plan["steps"]) or "Nothing to do: every figure is at zero.",
        "## The detail behind the first targets",
        *[targetSection(target) for target in plan["targets"][:DETAILED_TARGETS]],
    ]) + "\n"
