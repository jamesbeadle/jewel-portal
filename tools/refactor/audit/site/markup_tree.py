"""Builds a view's markup tree from its tokens: elements inside elements, template blocks around them."""
from __future__ import annotations

import re
from dataclasses import dataclass, field

from .markup_tokens import tokenize
from .tokens import BLOCK, CLOSE, END, OPEN, Token

ELEMENT, TEMPLATE_BLOCK, ROOT = "element", "block", "root"
VOID_ELEMENTS = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"}
CLASS_ATTRIBUTE = re.compile(r'\bclass\s*=\s*"([^"]*)"')
CLASS_NAME = re.compile(r"^[A-Za-z0-9:_\-\[\]./%]+$")


@dataclass
class Node:
    kind: str
    name: str = ""
    classes: list[str] = field(default_factory=list)
    attributes: str = ""
    isSelfClosing: bool = False
    condition: str = ""
    line: int = 0
    children: list["Node"] = field(default_factory=list)

    @property
    def isElement(self) -> bool:
        return self.kind == ELEMENT

    @property
    def isBlock(self) -> bool:
        return self.kind == TEMPLATE_BLOCK


def classesOf(attributes: str) -> list[str]:
    match = CLASS_ATTRIBUTE.search(attributes)
    if match is None:
        return []
    return [name for name in match.group(1).split() if CLASS_NAME.match(name)]


def elementNode(token: Token) -> Node:
    return Node(
        ELEMENT, name=token.name, classes=classesOf(token.attributes), attributes=token.attributes,
        isSelfClosing=token.isSelfClosing, line=token.line,
    )


def closesWithoutContent(token: Token) -> bool:
    return token.isSelfClosing or token.name.lower() in VOID_ELEMENTS


def popTo(stack: list[Node], isTarget) -> None:
    for depth in range(len(stack) - 1, 0, -1):
        if isTarget(stack[depth]):
            del stack[depth:]
            return


def place(stack: list[Node], token: Token) -> None:
    if token.kind == OPEN:
        node = elementNode(token)
        stack[-1].children.append(node)
        if not closesWithoutContent(token):
            stack.append(node)
        return
    if token.kind == CLOSE:
        popTo(stack, lambda node: node.isElement and node.name == token.name)
        return
    if token.kind == BLOCK:
        node = Node(TEMPLATE_BLOCK, name=token.name, condition=token.condition, line=token.line)
        stack[-1].children.append(node)
        stack.append(node)
        return
    if token.kind == END:
        popTo(stack, lambda node: node.isBlock)


def buildTree(text: str) -> Node:
    root = Node(ROOT)
    stack = [root]
    for token in tokenize(text):
        place(stack, token)
    return root
