"""The tokens a view's markup is read into: a tag opening or closing, a template block opening or ending."""
from __future__ import annotations

from dataclasses import dataclass

OPEN, CLOSE, BLOCK, END = "open", "close", "block", "end"


@dataclass(frozen=True)
class Token:
    kind: str
    name: str = ""
    attributes: str = ""
    isSelfClosing: bool = False
    condition: str = ""
    line: int = 0


def blockToken(kind: str, condition: str, line: int) -> Token:
    return Token(BLOCK, name=kind, condition=" ".join(condition.split()), line=line)


def endToken(line: int) -> Token:
    return Token(END, line=line)
