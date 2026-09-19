"""How a definition takes shape: sibling branches grouped as one choice, identical neighbours collapsed into a repeat."""
from __future__ import annotations

from .definition_kinds import REPEAT, Definition
from .markup_tree import Node

CONTINUING_BRANCHES = {"else if", "else"}


def branchGroups(children: list[Node]) -> list[list[Node]]:
    groups: list[list[Node]] = []
    for child in children:
        isContinuation = child.isBlock and child.name in CONTINUING_BRANCHES and groups and groups[-1][0].isBlock
        if isContinuation:
            groups[-1].append(child)
            continue
        groups.append([child])
    return groups


def collapseRepeats(items: list[Definition]) -> list[Definition]:
    collapsed: list[Definition] = []
    for item in items:
        previous = collapsed[-1] if collapsed else None
        if previous is not None and previous.kind == REPEAT and previous.count.isdigit() and previous.children[0].key() == item.key():
            previous.count = str(int(previous.count) + 1)
            continue
        if previous is not None and previous.key() == item.key():
            collapsed[-1] = Definition(REPEAT, count="2", children=[item])
            continue
        collapsed.append(item)
    return collapsed
