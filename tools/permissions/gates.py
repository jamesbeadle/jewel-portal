"""Resolving a gate to the roles it admits.

An endpoint gates in one of three shapes and all three answer the same question: an inline
`SomeRoles.Set.IncludesAny(user.Roles)`, `AdminGate.Allows(user)`, or an injected `*Authorisation`
field whose type holds the RoleSet."""
from __future__ import annotations

import re

ROLE_CHECK = re.compile(r"(?:(\w+)\.)?(\w+)\.Includes(?:Any)?\s*\(")
ROLE_LITERAL = re.compile(r"Roles\.Contains\(\s*Role\.(\w+)\s*\)")
ADMIN_GATE = re.compile(r"AdminGate\.Allows\s*\(")
EVERY_ROLE = re.compile(r"Enum\.GetValues<Role>\(\)")
GATE_CALL = re.compile(r"\b(\w+)\.(Allows|Authorise|Authorize|Permits|May\w*|Requires?\w*)\s*\(")
GATE_TYPE_NAME = re.compile(r"(Authorisation|Authorization|Gates?|Roles|Policy)$")
QUALIFIED_TYPE = re.compile(r"\b([A-Z]\w+)\s*\.")

ADMIN_EQUIVALENT = ("Admin", "FinanceDirector")


def lookUp(reference: str, sets: dict, owner: str | None = None) -> list[str] | None:
    if owner and f"{owner}.{reference}" in sets:
        return sets[f"{owner}.{reference}"]["roles"]
    if reference in sets:
        return sets[reference]["roles"]
    tail = reference.split(".")[-1]
    matching = [value["roles"] for key, value in sets.items() if key.split(".")[-1] == tail]
    if not matching:
        return None
    first = sorted(matching[0])
    return matching[0] if all(sorted(other) == first for other in matching) else None


class Resolver:
    """Answers which roles a body of C#, or a named gate type, admits."""

    def __init__(self, sets: dict, everyRole: list[str], typeBodies: dict[str, str]):
        self.sets = sets
        self.everyRole = list(everyRole)
        self.typeBodies = typeBodies

    def ofBody(self, body: str, seen: frozenset = frozenset(), owner: str | None = None) -> dict:
        allowed: set[str] = set()
        sources: set[str] = set()
        unresolved: set[str] = set()
        if EVERY_ROLE.search(body):
            allowed.update(self.everyRole)
            sources.add("every role")
        if ADMIN_GATE.search(body):
            allowed.update(ADMIN_EQUIVALENT)
            sources.add("AdminGate")
        for role in ROLE_LITERAL.findall(body):
            allowed.add(role)
            sources.add(f"Roles.Contains({role})")
        for qualifier, name in ROLE_CHECK.findall(body):
            reference = f"{qualifier}.{name}" if qualifier else name
            sources.add(reference)
            roles = lookUp(reference, self.sets, owner)
            unresolved.add(reference) if roles is None else allowed.update(roles)
        self.followGateTypes(body, allowed, sources, unresolved, seen)
        return {"roles": sorted(allowed), "sources": sorted(sources), "unresolved": sorted(unresolved)}

    def followGateTypes(self, body: str, allowed: set, sources: set, unresolved: set,
                        seen: frozenset) -> None:
        for typeName in self.gateTypesNamedIn(body) - seen:
            onward = self.ofType(typeName, seen | {typeName})
            if onward is None:
                continue
            allowed.update(onward["roles"])
            sources.add(typeName)
            unresolved.update(onward["unresolved"])

    def gateTypesNamedIn(self, body: str) -> set[str]:
        named = {name for name, _ in GATE_CALL.findall(body) if name in self.typeBodies}
        named.update(name for name in QUALIFIED_TYPE.findall(body)
                     if GATE_TYPE_NAME.search(name) and name in self.typeBodies)
        return named

    def ofType(self, typeName: str, seen: frozenset = frozenset()) -> dict | None:
        body = self.typeBodies.get(typeName)
        if body is None:
            return None
        outcome = self.ofBody(body, seen | {typeName}, typeName)
        return outcome if outcome["roles"] else None
