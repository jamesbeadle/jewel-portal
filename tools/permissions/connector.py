"""The MCP connector's surface: the tools a role may call, and the actions it may perform.

An action carries two role sets — VisibleTo, which decides what the model is offered, and the
AuthorisationType it actually runs through. The check is that the first never exceeds the second."""
from __future__ import annotations

import re
from pathlib import Path

from . import gates, masking, source

TOOL = re.compile(r'new\s*(?:AiTool)?\(\s*"(\w+)"', re.S)
TOOL_ROLES = re.compile(r"AiToolKind\.\w+\s*,\s*([\w.]+)\s*,", re.S)
ACTION = re.compile(r"new\s+AiAction\s*\(")
NAMED_ARGUMENT = re.compile(r"(\w+):\s*([^,\n]+)")
TYPE_OF = re.compile(r"typeof\((\w+)\)")
TOOL_WINDOW = 4000


def argumentsAt(text: str, opening: int) -> str:
    depth = 0
    index = opening
    while index < len(text):
        if text[index] == "(":
            depth += 1
        elif text[index] == ")":
            if depth == 0:
                return text[opening:index]
            depth -= 1
        index += 1
    return text[opening:]


def tools(repositoryRoot: Path, resolver: gates.Resolver) -> list[dict]:
    found: list[dict] = []
    for path in source.connectorFiles(repositoryRoot):
        text = path.read_text(errors="replace")
        masked = masking.mask(text)
        starts = [match for match in TOOL.finditer(text)]
        for index, match in enumerate(starts):
            roles = TOOL_ROLES.search(masked[match.end():match.end() + TOOL_WINDOW])
            following = starts[index + 1].start() if index + 1 < len(starts) else len(text)
            found.append({
                "name": match.group(1),
                "file": path.relative_to(repositoryRoot).as_posix(),
                "roles": resolver.ofBody(f"{roles.group(1)}.IncludesAny()")["roles"] if roles else [],
                "body": masked[match.end():following],
            })
    return found


def actions(repositoryRoot: Path, resolver: gates.Resolver) -> list[dict]:
    found: list[dict] = []
    for path in source.connectorFiles(repositoryRoot):
        text = path.read_text(errors="replace")
        for match in ACTION.finditer(text):
            named = dict(NAMED_ARGUMENT.findall(argumentsAt(text, match.end())))
            name = named.get("Name", "").strip().strip('"')
            if not name:
                continue
            visibleTo = named.get("VisibleTo", "").strip()
            authorisation = TYPE_OF.sub(r"\1", named.get("AuthorisationType", "").strip())
            enforced = resolver.ofType(authorisation) if authorisation else None
            found.append({
                "name": name,
                "file": path.relative_to(repositoryRoot).as_posix(),
                "visibleTo": visibleTo,
                "visibleRoles": resolver.ofBody(f"{visibleTo}.IncludesAny()")["roles"] if visibleTo else [],
                "authorisation": authorisation,
                "authorisedRoles": enforced["roles"] if enforced else [],
            })
    return found
