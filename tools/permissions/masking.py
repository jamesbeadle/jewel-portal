"""A copy of a C# file with comments and string contents blanked out, character for character.

Structure is read from the mask and sliced from the original, so prose can never be mistaken for
code. Before this existed, the words "the audit record of why" inside a validation message declared
a type called `of`, and that type's body carried one file's role gates into another file's answer.
"""
from __future__ import annotations


def mask(text: str) -> str:
    blanked: list[str] = []
    index = 0
    while index < len(text):
        character = text[index]
        if text.startswith("//", index):
            index = blankRun(text, index, "\n", blanked)
        elif text.startswith("/*", index):
            index = blankRun(text, index, "*/", blanked)
        elif text.startswith('@"', index):
            index = blankVerbatim(text, index, blanked)
        elif character in "\"'":
            index = blankQuoted(text, index, blanked)
        else:
            blanked.append(character)
            index += 1
    return "".join(blanked)


def blankRun(text: str, start: int, terminator: str, blanked: list[str]) -> int:
    end = text.find(terminator, start + 2)
    end = len(text) if end < 0 else end + len(terminator)
    blanked.extend("\n" if character == "\n" else " " for character in text[start:end])
    return end


def blankQuoted(text: str, start: int, blanked: list[str]) -> int:
    quote = text[start]
    blanked.append(quote)
    index = start + 1
    while index < len(text):
        if text[index] == "\\" and index + 1 < len(text):
            blanked.append("  ")
            index += 2
            continue
        if text[index] == quote:
            blanked.append(quote)
            return index + 1
        blanked.append("\n" if text[index] == "\n" else " ")
        index += 1
    return index


def blankVerbatim(text: str, start: int, blanked: list[str]) -> int:
    blanked.append('@"')
    index = start + 2
    while index < len(text):
        if text.startswith('""', index):
            blanked.append("  ")
            index += 2
            continue
        if text[index] == '"':
            blanked.append('"')
            return index + 1
        blanked.append("\n" if text[index] == "\n" else " ")
        index += 1
    return index
