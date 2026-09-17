"""Splits names and paths into their lower-case words, whatever the casing convention."""
from __future__ import annotations

import re

WORD = re.compile(r"[A-Z]+(?![a-z])|[A-Z]?[a-z]+|\d+")


def wordsOf(text: str) -> list[str]:
    return [word.lower() for word in WORD.findall(text)]


def pluralsOf(word: str) -> set[str]:
    if word.endswith("y"):
        return {word, word[:-1] + "ies"}
    return {word, word + "s", word + "es"}


def containsPhrase(haystack: list[str], phrase: list[str]) -> bool:
    if not phrase or len(phrase) > len(haystack):
        return False
    lastWordForms = pluralsOf(phrase[-1])
    for start in range(len(haystack) - len(phrase) + 1):
        window = haystack[start:start + len(phrase)]
        if window[:-1] == phrase[:-1] and window[-1] in lastWordForms:
            return True
    return False
