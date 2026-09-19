"""Reads a view's markup as a stream of tokens: tags opening and closing, and the template blocks between them.

Razor (`@if`, `@foreach`, `@(…)`) and Svelte (`{#if}`, `{#each}`) both pass through; an attribute value may
hold an expression with quotes of its own, so the reader walks the text rather than matching a pattern.
"""
from __future__ import annotations

import re

from .markup_portion import lineOf, markupPortion
from .razor_expressions import BLOCK_OPENING, ELSE_BRANCH, SVELTE_BLOCK, skipBalanced, skipRazorExpression
from .tag_reader import readTag
from .tokens import BLOCK, END, Token, blockToken, endToken

TAG_START = re.compile(r"<(?:/?[A-Za-z]|!)")
OPENING_BRACE = re.compile(r"\s*\{")
OPENING_PARENTHESIS = re.compile(r"\s*\(")
SVELTE_CLOSES, SVELTE_CONTINUES = "/", ":"


def readBlockOpening(text: str, position: int, opening: re.Match) -> tuple[list[Token], int]:
    cursor = opening.end()
    condition = ""
    parenthesis = OPENING_PARENTHESIS.match(text, cursor)
    if parenthesis:
        conditionEnd = skipBalanced(text, parenthesis.end() - 1, "(", ")")
        condition = text[parenthesis.end() : conditionEnd - 1]
        cursor = conditionEnd
    brace = OPENING_BRACE.match(text, cursor)
    if brace is None:
        return [], cursor
    return [blockToken(opening.group(1), condition, lineOf(text, position))], brace.end()


def readRazor(text: str, position: int) -> tuple[list[Token], int]:
    opening = BLOCK_OPENING.match(text, position)
    if opening:
        return readBlockOpening(text, position, opening)
    return [], skipRazorExpression(text, position)


def readBlockEnd(text: str, position: int) -> tuple[list[Token], int]:
    ended = [endToken(lineOf(text, position))]
    elseBranch = ELSE_BRANCH.match(text, position + 1)
    if elseBranch is None:
        return ended, position + 1
    kind = "else if" if elseBranch.group(1) else "else"
    return ended + [blockToken(kind, elseBranch.group(1) or "", lineOf(text, position))], elseBranch.end()


def readSvelteBlock(text: str, position: int) -> tuple[list[Token], int]:
    match = SVELTE_BLOCK.match(text, position)
    if match is None:
        return [], position + 1
    line = lineOf(text, position)
    if match.group(1) == SVELTE_CLOSES:
        return [endToken(line)], match.end()
    opened = blockToken(match.group(2), match.group(3) or "", line)
    if match.group(1) == SVELTE_CONTINUES:
        return [endToken(line), opened], match.end()
    return [opened], match.end()


def readNext(markup: str, position: int, openBlocks: int) -> tuple[list[Token], int]:
    character = markup[position]
    if character == "<" and TAG_START.match(markup, position):
        token, end = readTag(markup, position, lineOf(markup, position))
        return ([token] if token else []), end
    if character == "@":
        return readRazor(markup, position)
    if character == "}" and openBlocks > 0:
        return readBlockEnd(markup, position)
    if character == "{":
        return readSvelteBlock(markup, position)
    return [], position + 1


def tokenize(text: str) -> list[Token]:
    markup = markupPortion(text)
    tokens: list[Token] = []
    openBlocks = 0
    position = 0
    while position < len(markup):
        read, position = readNext(markup, position, openBlocks)
        tokens += read
        openBlocks += sum({BLOCK: 1, END: -1}.get(token.kind, 0) for token in read)
    return tokens
