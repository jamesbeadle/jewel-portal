"""Cuts a view down to the markup the reader walks: no razor comments, no directive lines, nothing after the code section.

Every cut keeps its line breaks, so a position in the markup still says which line of the file it is on.
"""
from __future__ import annotations

import re

RAZOR_COMMENT = re.compile(r"@\*.*?\*@", re.DOTALL)
CODE_SECTION = re.compile(r"^[ \t]*@(?:code|functions)\b", re.MULTILINE)
DIRECTIVE_LINE = re.compile(
    r"^[ \t]*@(?:page|using|inject|inherits|implements|layout|attribute|typeparam|namespace|model|rendermode)\b.*$",
    re.MULTILINE,
)


def onlyLineBreaksOf(match: re.Match) -> str:
    return "\n" * match.group(0).count("\n")


def markupPortion(text: str) -> str:
    withoutComments = RAZOR_COMMENT.sub(onlyLineBreaksOf, text)
    codeSection = CODE_SECTION.search(withoutComments)
    markup = withoutComments[: codeSection.start()] if codeSection else withoutComments
    return DIRECTIVE_LINE.sub("", markup)


def lineOf(text: str, position: int) -> int:
    return text.count("\n", 0, position) + 1
