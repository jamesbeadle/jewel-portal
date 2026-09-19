"""Writes a definition in the site's notation: `a, b` stacked, `[ a b ]` side by side, `(3) a` repeated, `?when: a` conditional, `a | b` one or the other.

`Widget{ … }` is a catalogue widget with content, `Part=( … )` one of the site's own components opened out, `⚠table→RecordsTable`
markup written by hand where the named widget should be.
"""
from __future__ import annotations

from .definition_kinds import COLUMN, EITHER, FINDING, LEAF, MAYBE, PART, REPEAT, ROW, WIDGET, Definition

STACKED, SIDE_BY_SIDE = ", ", " "
CONDITION_LENGTH = 32
EXPANSION_DEPTH = 3
HAND_ROLLED_MARK = "⚠"


def shortCondition(condition: str) -> str:
    if len(condition) <= CONDITION_LENGTH:
        return condition
    return condition[: CONDITION_LENGTH - 1] + "…"


def grouped(items: list[Definition], separator: str) -> str:
    rendered = separator.join(render(item) for item in items)
    return rendered if len(items) == 1 else f"( {rendered} )"


def renderMaybe(definition: Definition) -> str:
    inner = grouped(definition.children, STACKED)
    if not definition.condition:
        return inner
    return f"?{shortCondition(definition.condition)}: {inner}"


def renderNamed(definition: Definition) -> str:
    if not definition.children:
        return definition.name
    return f"{definition.name}{{ {STACKED.join(render(child) for child in definition.children)} }}"


def render(definition: Definition) -> str:
    kind = definition.kind
    if kind in (WIDGET, PART, LEAF):
        return renderNamed(definition)
    if kind == FINDING:
        return f"{HAND_ROLLED_MARK}{definition.name}→{definition.condition}"
    if kind == ROW:
        return f"[ {SIDE_BY_SIDE.join(render(child) for child in definition.children)} ]"
    if kind == COLUMN:
        return grouped(definition.children, STACKED)
    if kind == REPEAT:
        return f"({definition.count}) {grouped(definition.children, STACKED)}"
    if kind == MAYBE:
        return renderMaybe(definition)
    return f"( {' | '.join(render(child) for child in definition.children)} )" if kind == EITHER else ""


def renderStacked(definitions: list[Definition]) -> str:
    return STACKED.join(render(definition) for definition in definitions)


def expanded(definition: Definition, parts: dict[str, list[Definition]], lineage: tuple[str, ...]) -> Definition:
    children = [expanded(child, parts, lineage) for child in definition.children]
    isOpenable = definition.kind == PART and definition.name in parts and definition.name not in lineage and len(lineage) < EXPANSION_DEPTH
    if not isOpenable:
        return Definition(definition.kind, definition.name, definition.condition, definition.count, definition.line, children)
    opened = [expanded(child, parts, lineage + (definition.name,)) for child in parts[definition.name]]
    name = f"{definition.name}=( {renderStacked(opened)} )" if opened else definition.name
    return Definition(PART, name, definition.condition, definition.count, definition.line, children)


def renderExpanded(definitions: list[Definition], parts: dict[str, list[Definition]]) -> str:
    return renderStacked([expanded(definition, parts, ()) for definition in definitions])
