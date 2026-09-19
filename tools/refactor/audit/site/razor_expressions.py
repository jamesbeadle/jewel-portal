"""Steps over the expressions a template embeds in its markup, so a quote or brace inside one is never read as markup."""
from __future__ import annotations

import re

BLOCK_OPENING = re.compile(r"@(else if|if|foreach|for|switch|while|lock|using)\b")
ELSE_BRANCH = re.compile(r"\s*else(?:\s+if\s*\(([^{]*)\))?\s*\{")
SVELTE_BLOCK = re.compile(r"\{(?:(#|:|/)\s*(if|each|await|key|else if|else|then|catch)\b([^}]*))\}")
IDENTIFIER = re.compile(r"[A-Za-z_][\w]*")
MEMBER_ACCESS = re.compile(r"\.[A-Za-z_]")
STRING_QUOTES = ('"', "'")
ESCAPE = "\\"
OPENERS = {"(": ")", "[": "]", "{": "}"}


def skipQuotedString(text: str, position: int) -> int:
    quote = text[position]
    cursor = position + 1
    while cursor < len(text) and text[cursor] != quote:
        cursor += 2 if text[cursor] == ESCAPE else 1
    return cursor + 1


def skipBalanced(text: str, position: int, opener: str, closer: str) -> int:
    depth = 0
    cursor = position
    while cursor < len(text):
        character = text[cursor]
        if character in STRING_QUOTES:
            cursor = skipQuotedString(text, cursor)
            continue
        depth += (character == opener) - (character == closer)
        cursor += 1
        if depth == 0:
            return cursor
    return cursor


def skipMemberChain(text: str, position: int) -> int:
    cursor = position
    while cursor < len(text):
        if text[cursor] in OPENERS:
            cursor = skipBalanced(text, cursor, text[cursor], OPENERS[text[cursor]])
            continue
        member = MEMBER_ACCESS.match(text, cursor)
        if member is None:
            return cursor
        identifier = IDENTIFIER.match(text, cursor + 1)
        cursor = identifier.end()
    return cursor


def skipRazorExpression(text: str, position: int) -> int:
    following = text[position + 1 : position + 2]
    if following in OPENERS:
        return skipBalanced(text, position + 1, following, OPENERS[following])
    if following in ("@", ":"):
        return position + 2
    identifier = IDENTIFIER.match(text, position + 1)
    if identifier is None:
        return position + 1
    return skipMemberChain(text, identifier.end())
