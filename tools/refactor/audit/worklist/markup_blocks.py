"""Finds the blocks of markup in a view that are large enough to be a component of their own."""
from __future__ import annotations

import re

OPENING = re.compile(r"^\s*(?:<[A-Za-z][\w\.:-]*|\{#\w+|@(?:if|foreach|for|switch)\b)")
CLOSER = re.compile(r"^\s*(?:</|\{/|\})")
CONTINUATION = re.compile(r"^\s*(?:\{:|\{\s*$|else\b|\}\s*else\b)")
SCRIPT_OR_STYLE_OPENING = re.compile(r"^\s*<(script|style)\b")
RAZOR_CODE_BLOCK = re.compile(r"^\s*@(?:code|functions)\b")
LABEL_LENGTH = 80


def indentation(line: str) -> int:
    return len(line) - len(line.lstrip(" \t"))


def markupLineNumbers(lines: list[str]) -> list[int]:
    numbers = []
    closingTag = None
    for number, line in enumerate(lines):
        if RAZOR_CODE_BLOCK.match(line):
            return numbers
        opening = SCRIPT_OR_STYLE_OPENING.match(line)
        if opening and closingTag is None:
            closingTag = f"</{opening.group(1)}>"
        if closingTag is None:
            numbers.append(number)
        if closingTag and closingTag in line:
            closingTag = None
    return numbers


def blockEnd(lines: list[str], start: int, last: int) -> int:
    for number in range(start + 1, last + 1):
        line = lines[number]
        if not line.strip() or CONTINUATION.match(line) or indentation(line) > indentation(lines[start]):
            continue
        return number if CLOSER.match(line) else number - 1
    return last


def breakoutBlocks(lines: list[str], first: int, last: int, minimumLines: int, maximumLines: int) -> list[dict]:
    blocks = []
    number = first
    while number <= last:
        if not OPENING.match(lines[number]):
            number += 1
            continue
        end = blockEnd(lines, number, last)
        length = end - number + 1
        whole = {"opens": lines[number].strip()[:LABEL_LENGTH], "firstLine": number + 1, "lastLine": end + 1, "lines": length}
        if length > maximumLines:
            blocks += breakoutBlocks(lines, number + 1, end - 1, minimumLines, maximumLines) or [whole]
        if minimumLines <= length <= maximumLines:
            blocks.append(whole)
        number = end + 1
    return blocks


def componentCandidates(lines: list[str], minimumLines: int, maximumLines: int) -> list[dict]:
    markup = markupLineNumbers(lines)
    if not markup:
        return []
    return breakoutBlocks(lines, markup[0], markup[-1], minimumLines, maximumLines)
