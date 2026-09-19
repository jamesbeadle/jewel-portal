"""The little of C# this check has to understand: where a type ends, where a member's body begins,
and what a field was declared as. Structure is matched on the mask and sliced from the original."""
from __future__ import annotations

import re

from . import masking

TYPE_DECLARATION = re.compile(r"\b(?:class|record|struct)\s+(\w+)")
FIELD_DECLARATION = re.compile(
    r"(?:private|internal|public|protected)\s+(?:static\s+)?readonly\s+([\w.<>,\s\?]+?)\s+(\w+)\s*[;=]")
MEMBER_DECLARATION = re.compile(
    r"(?:private|internal|public|protected)\s+(?:static\s+|async\s+|sealed\s+|override\s+)*"
    r"[\w.<>,\[\]\?\s]+?\s+(\w+)\s*\([^;{]*?\)\s*(?==>|\{)", re.S)
INVOCATION = re.compile(r"\b(\w+)\s*\(")


def blockAt(text: str, opening: int) -> str:
    depth = 0
    index = opening
    while index < len(text):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[opening:index + 1]
        index += 1
    return text[opening:]


def bodyAfter(text: str, position: int) -> str:
    arrow = text.find("=>", position)
    opening = text.find("{", position)
    if arrow >= 0 and (opening < 0 or arrow < opening) and arrow - position < 4:
        end = text.find(";", arrow)
        return text[arrow:end if end > 0 else len(text)]
    return blockAt(text, opening) if opening >= 0 else ""


def bodyAfterParameters(text: str, insideParameters: int) -> str:
    """The body of the member whose parameter list contains this position — the shape an endpoint
    takes, since the HttpTrigger attribute sits on its first parameter."""
    depth = 0
    index = insideParameters
    while index < len(text) and not (text[index] == ")" and depth == 0):
        depth += 1 if text[index] == "(" else -1 if text[index] == ")" else 0
        index += 1
    remainder = text[index + 1:]
    offset = index + 1 + (len(remainder) - len(remainder.lstrip()))
    if remainder.lstrip().startswith("=>"):
        end = text.find(";", offset)
        return text[offset:end if end > 0 else len(text)]
    opening = text.find("{", offset)
    return blockAt(text, opening) if opening >= 0 else ""


def types(text: str):
    masked = masking.mask(text)
    for match in TYPE_DECLARATION.finditer(masked):
        opening = masked.find("{", match.end())
        if opening >= 0:
            yield match.group(1), text[opening:opening + len(blockAt(masked, opening))]


def members(typeBody: str) -> dict[str, str]:
    masked = masking.mask(typeBody)
    found: dict[str, str] = {}
    for match in MEMBER_DECLARATION.finditer(masked):
        body = bodyAfter(masked, match.end())
        start = masked.find(body, match.end()) if body else -1
        found.setdefault(match.group(1), typeBody[start:start + len(body)] if start >= 0 else body)
    return found


def fields(typeBody: str) -> dict[str, str]:
    return {name: declared.strip()
            for declared, name in FIELD_DECLARATION.findall(masking.mask(typeBody))}


def reachableBody(entryBody: str, siblings: dict[str, str], depth: int = 4) -> str:
    """The entry body plus the bodies of the sibling members it calls, so a gate that lives in a
    private Gate() helper is found where the endpoint is."""
    collected = [entryBody]
    pending = [(entryBody, depth)]
    seen: set[str] = set()
    while pending:
        body, remaining = pending.pop()
        if remaining <= 0:
            continue
        for name in set(INVOCATION.findall(body)) - seen:
            if name not in siblings:
                continue
            seen.add(name)
            collected.append(siblings[name])
            pending.append((siblings[name], remaining - 1))
    return "\n".join(collected)
