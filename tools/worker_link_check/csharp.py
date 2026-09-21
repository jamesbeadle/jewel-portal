"""Reading C# source well enough to say which types a file declares and which it reaches for.

Not a parser: regular expressions over source with its comments and literal text removed, which
is enough for the one question this tool asks and cheap enough to run before every commit.

Two things this has to get right, because getting either wrong hides a real failure or blocks a
good commit:

An interpolated string is not literal text. `$"...{PrivacyNoticeLink.Path}..."` reaches for a
type as surely as a bare call does, and dropping the whole literal is what hid the worker break
of 21 September 2026 from an earlier draft of this tool. The braces are kept, the prose between
them is dropped.

A name in type position is a type; a name in member position is not. `Outcome Result` names the
type `Outcome`; `string Outcome` names a member. Telling the two apart by which side of the pair
the name sits on is what keeps a property called `Outcome`, `Model` or `Count` from being
mistaken for the api type of that name.
"""

import re

DECLARATION = re.compile(
    r"^\s*(?:public|internal|private|protected)?\s*"
    r"(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|ref\s+|file\s+)*"
    r"(?:class|record|struct|interface|enum)\s+(\w+)",
    re.MULTILINE,
)

COMMENT = re.compile(r"//[^\n]*|/\*.*?\*/", re.DOTALL)
STRING_LITERAL = re.compile(r'\$?@?\$?"(?:[^"\\]|\\.)*"')
INTERPOLATION = re.compile(r"\{([^{}]*)\}")

TYPE_NAME = r"(?<![.\w])([A-Z]\w*)"
STATIC_MEMBER = re.compile(TYPE_NAME + r"\s*\.")
CONSTRUCTION = re.compile(r"\bnew\s+([A-Z]\w*)")
TYPE_POSITION = re.compile(TYPE_NAME + r"[?]?(?:\[\])?\s+\w+\s*[;=,){]")
TYPE_ARGUMENT = re.compile(r"[<,]\s*([A-Z]\w*)[?]?\s*[>,]")


def read(path):
    return path.read_text(encoding="utf-8", errors="ignore")


def declared_types(paths):
    types = set()
    for path in paths:
        types.update(DECLARATION.findall(read(path)))
    return types


def _without_its_prose(literal):
    return " ".join(INTERPOLATION.findall(literal))


def code_only(source):
    return STRING_LITERAL.sub(lambda match: _without_its_prose(match.group()), COMMENT.sub("", source))


def names_reached_for(source):
    return {
        name
        for pattern in (STATIC_MEMBER, CONSTRUCTION, TYPE_POSITION, TYPE_ARGUMENT)
        for name in pattern.findall(source)
    }
