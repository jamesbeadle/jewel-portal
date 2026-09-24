"""What an endpoint reaches, followed through the types it is handed.

The correspondence rule used to be judged by route, so an endpoint whose route does not SAY mail
could hand it out: the RFI conversation merged the tagged emails into `requests/{id}/messages` and
admitted the architect (2026-09-24). This follows each endpoint through its injected fields — a
query or command handler to the class that handles it, a service to its own fields — so the rule
is judged by what the endpoint actually reads."""
from __future__ import annotations

import re

from . import csharp, masking, source

HANDLER = re.compile(r"I(?:Query|Command)Handler<\s*(\w+)\s*,")
DECLARATION_HEADER = re.compile(r"\b(?:class|record|struct)\s+(\w+)([^{;]*)")
TYPE_NAME = re.compile(r"\b([A-Z]\w+)\b")
MAXIMUM_DEPTH = 5
REFUSES_AN_EXTERNAL_CALLER = "MayReadInternalCorrespondence"


def handlerOf(repositoryRoot) -> dict[str, str]:
    """The class that handles each query or command, by the query's name — read off the base
    list, which lives before the body csharp.types returns."""
    found: dict[str, str] = {}
    for path in source.apiFiles(repositoryRoot):
        for match in DECLARATION_HEADER.finditer(masking.mask(path.read_text(errors="replace"))):
            for handled in HANDLER.findall(match.group(2)):
                found.setdefault(handled, match.group(1))
    return found


def referencedTypes(typeBody: str, handlers: dict[str, str], known: set[str]) -> set[str]:
    referenced: set[str] = set()
    for declared in csharp.fields(typeBody).values():
        for handled in HANDLER.findall(declared):
            if handled in handlers:
                referenced.add(handlers[handled])
        referenced.update(name for name in TYPE_NAME.findall(declared) if name in known)
    return referenced


def reaches(start: str, merged: dict[str, str], handlers: dict[str, str], targets: set[str],
            stopAt: frozenset[str] = frozenset()) -> str | None:
    """The first target type the start type reaches through its fields, or None. A type in
    `stopAt` is not followed any further."""
    known = set(merged)
    seen = {start}
    frontier = [start]
    for _ in range(MAXIMUM_DEPTH):
        following: list[str] = []
        for typeName in frontier:
            for referenced in referencedTypes(merged.get(typeName, ""), handlers, known | targets):
                if referenced in targets:
                    return referenced
                if referenced not in seen and referenced not in stopAt:
                    seen.add(referenced)
                    following.append(referenced)
        frontier = following
    return None


class MailReaders:
    """Which unguarded mail reader a type or a piece of code reaches: the readers named for any
    verb, and — for a read — the mailbox client itself. A reader whose own code refuses an
    external caller is guarded and never reported; one that stops asking is reported again."""

    def __init__(self, repositoryRoot, byName: dict, readers: dict):
        from . import endpoints
        self.merged = endpoints.mergedBodies(byName)
        self.handlers = handlerOf(repositoryRoot)
        guarded = {reader for reader in readers["anyVerb"]
                   if REFUSES_AN_EXTERNAL_CALLER in self.merged.get(reader, "")}
        self.guarded = frozenset(guarded)
        self.anyVerb = set(readers["anyVerb"]) - guarded
        self.everyReader = self.anyVerb | set(readers["reads"])

    def ofEndpoint(self, typeName: str, verbs: list[str]) -> str | None:
        targets = self.everyReader if "get" in verbs else self.anyVerb
        return reaches(typeName, self.merged, self.handlers, targets, self.guarded)

    def ofCode(self, code: str) -> str | None:
        named = set(TYPE_NAME.findall(code))
        direct = named & self.anyVerb
        if direct:
            return sorted(direct)[0]
        for typeName in sorted(named & set(self.merged)):
            reader = reaches(typeName, self.merged, self.handlers, self.anyVerb, self.guarded)
            if reader:
                return reader
        return None
