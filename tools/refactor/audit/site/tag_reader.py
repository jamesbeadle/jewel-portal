"""Reads one tag out of a view's markup, attribute by attribute, so an expression inside an attribute value stays inside it."""
from __future__ import annotations

import re

from .razor_expressions import skipBalanced
from .tokens import CLOSE, OPEN, Token

TAG_NAME = re.compile(r"[A-Za-z][\w.:-]*")
ATTRIBUTE_NAME = re.compile(r"[^\s=>/]+")
COMMENT_END = "-->"
STRING_QUOTES = ('"', "'")
EXPRESSION_OPENING = "@("


def readComment(text: str, position: int) -> int:
    end = text.find(COMMENT_END, position)
    return len(text) if end < 0 else end + len(COMMENT_END)


def readQuotedValue(text: str, position: int) -> int:
    quote = text[position]
    cursor = position + 1
    while cursor < len(text):
        if text.startswith(EXPRESSION_OPENING, cursor):
            cursor = skipBalanced(text, cursor + 1, "(", ")")
            continue
        if text[cursor] == quote:
            return cursor + 1
        cursor += 1
    return cursor


def readUnquotedValue(text: str, position: int) -> int:
    cursor = position
    while cursor < len(text) and not text[cursor].isspace() and text[cursor] != ">":
        cursor += 1
    return cursor


def readAttributeValue(text: str, position: int) -> int:
    if text[position] in STRING_QUOTES:
        return readQuotedValue(text, position)
    return readUnquotedValue(text, position)


def readOneAttribute(text: str, position: int) -> int:
    name = ATTRIBUTE_NAME.match(text, position)
    cursor = name.end() if name else position + 1
    hasValue = cursor < len(text) and text[cursor] == "="
    if hasValue and cursor + 1 < len(text):
        return readAttributeValue(text, cursor + 1)
    return cursor + 1 if hasValue else cursor


def readAttributes(text: str, position: int) -> tuple[str, bool, int]:
    cursor = position
    while cursor < len(text):
        while cursor < len(text) and text[cursor].isspace():
            cursor += 1
        if text.startswith("/>", cursor):
            return text[position:cursor], True, cursor + 2
        if text.startswith(">", cursor):
            return text[position:cursor], False, cursor + 1
        cursor = readOneAttribute(text, cursor)
    return text[position:cursor], False, cursor


def readTag(text: str, position: int, line: int) -> tuple[Token | None, int]:
    if text.startswith("<!--", position):
        return None, readComment(text, position)
    if text.startswith("<!", position):
        return None, text.find(">", position) + 1
    isClosing = text.startswith("</", position)
    name = TAG_NAME.match(text, position + (2 if isClosing else 1))
    if name is None:
        return None, position + 1
    if isClosing:
        return Token(CLOSE, name=name.group(0), line=line), text.find(">", name.end()) + 1
    attributes, isSelfClosing, end = readAttributes(text, name.end())
    return Token(OPEN, name=name.group(0), attributes=attributes, isSelfClosing=isSelfClosing, line=line), end
