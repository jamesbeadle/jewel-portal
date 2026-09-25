"""Which block of markup owns a function: the test for whether a function moves out with the component.

A function belongs to a block when the block mentions it and nothing else in the file does, so breaking the
block out takes the function with it rather than leaving it behind in the parent.
"""
from __future__ import annotations

import re

from ..declarations import DeclaredFunction


def mentionsName(lines: list, name: str) -> bool:
    pattern = re.compile(rf"\b{re.escape(name)}\b")
    return any(pattern.search(line) for line in lines)


def blockOwning(function: DeclaredFunction, lines: list, blocks: list) -> dict | None:
    ownLines = range(function.line - 1, function.line - 1 + function.lines)
    for block in blocks:
        blockLines = range(block["firstLine"] - 1, block["lastLine"])
        inside = [lines[number] for number in blockLines]
        outside = [line for number, line in enumerate(lines) if number not in blockLines and number not in ownLines]
        if mentionsName(inside, function.name) and not mentionsName(outside, function.name):
            return block
    return None
