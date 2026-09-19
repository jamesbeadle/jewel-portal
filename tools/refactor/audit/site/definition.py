"""Reduces a view's markup tree to what a user sees: the widgets it is made of, how they stack, what repeats, what is written by hand."""
from __future__ import annotations

from .definition_kinds import COLUMN, EITHER, FINDING, LEAF, MAYBE, PART, REPEAT, ROW, WIDGET, Definition
from .definition_shape import branchGroups, collapseRepeats
from .markup_tree import Node
from .vocabulary import CATALOGUE, COMPONENT, HTML, IGNORED, Vocabulary

REPEATING_BLOCKS = {"foreach", "for", "while", "each"}
CHOOSING_BLOCKS = {"if", "else if", "else", "switch", "then", "catch"}
PER_ITEM = "n"


def reduceChildren(node: Node, vocabulary: Vocabulary, ancestors: tuple[str, ...]) -> list[Definition]:
    items: list[Definition] = []
    for group in branchGroups(node.children):
        items += reduceGroup(group, vocabulary, ancestors)
    return collapseRepeats(items)


def reduceGroup(group: list[Node], vocabulary: Vocabulary, ancestors: tuple[str, ...]) -> list[Definition]:
    if len(group) == 1:
        return reduceNode(group[0], vocabulary, ancestors)
    alternatives = [Definition(MAYBE, condition=branch.condition, children=reduceChildren(branch, vocabulary, ancestors)) for branch in group]
    shown = [alternative for alternative in alternatives if alternative.children]
    if len(shown) < 2:
        return shown
    return [Definition(EITHER, children=shown)]


def reduceBlock(node: Node, vocabulary: Vocabulary, ancestors: tuple[str, ...]) -> list[Definition]:
    children = reduceChildren(node, vocabulary, ancestors)
    if not children:
        return []
    if node.name in REPEATING_BLOCKS:
        return [Definition(REPEAT, count=PER_ITEM, children=children)]
    if node.name in CHOOSING_BLOCKS:
        return [Definition(MAYBE, condition=node.condition, children=children)]
    return children


def reduceHtml(node: Node, vocabulary: Vocabulary, ancestors: tuple[str, ...]) -> list[Definition]:
    owner = vocabulary.owningWidget(node)
    if owner is not None and owner not in ancestors:
        return [Definition(FINDING, name=node.name.lower(), condition=owner, line=node.line)]
    if not vocabulary.isHorizontal(node.classes):
        return reduceChildren(node, vocabulary, ancestors)
    cells = [asOneCell(reduceNode(child, vocabulary, ancestors)) for child in node.children]
    cells = [cell for cell in cells if cell is not None]
    return [Definition(ROW, children=collapseRepeats(cells))] if len(cells) > 1 else cells


def asOneCell(items: list[Definition]) -> Definition | None:
    if not items:
        return None
    return items[0] if len(items) == 1 else Definition(COLUMN, children=items)


def reduceNode(node: Node, vocabulary: Vocabulary, ancestors: tuple[str, ...]) -> list[Definition]:
    if node.isBlock:
        return reduceBlock(node, vocabulary, ancestors)
    kind = vocabulary.kindOf(node.name)
    if kind == IGNORED:
        return []
    if kind == HTML:
        return reduceHtml(node, vocabulary, ancestors)
    if kind in (CATALOGUE, COMPONENT):
        children = reduceChildren(node, vocabulary, ancestors + (node.name,))
        return [Definition(WIDGET if kind == CATALOGUE else PART, name=node.name, line=node.line, children=children)]
    if node.isSelfClosing:
        return [Definition(LEAF, name=node.name, line=node.line)]
    return reduceChildren(node, vocabulary, ancestors)


def reduceView(root: Node, vocabulary: Vocabulary, isCatalogueWidget: bool = False) -> list[Definition]:
    ownersPresent = vocabulary.everyOwner() if isCatalogueWidget else ()
    return reduceChildren(root, vocabulary, ownersPresent)
