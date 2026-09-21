"""The role vocabulary: the Role enum, the JpmsRoles aliases, and every RoleSet declared in
contracts or the API, each resolved to the concrete roles it admits."""
from __future__ import annotations

import re
from pathlib import Path

from . import csharp, masking, source

ROLE_MEMBER = re.compile(r"^([A-Z][A-Za-z]+)\s*(?:=\s*\d+\s*)?,?$")
ALIAS = re.compile(r"public const Role (\w+)\s*=\s*Role\.(\w+)\s*;")
DECLARATION = re.compile(
    r"(?:public|internal|private|protected)\s+static\s+(?:readonly\s+)?RoleSet\s+(\w+)\s*=\s*(.+?);",
    re.S)
NAMED_ROLE = re.compile(r"(?:Role|JpmsRoles)\.(\w+)")
EVERY_ROLE = re.compile(r"Enum\.GetValues<Role>\(\)")


def readRoles(repositoryRoot: Path) -> list[str]:
    text = masking.mask((repositoryRoot / "contracts/Models/Role.cs").read_text())
    body = text.split("{", 1)[1]
    matches = (ROLE_MEMBER.match(line.strip()) for line in body.splitlines())
    return [match.group(1) for match in matches if match and match.group(1) != "Role"]


def readAliases(repositoryRoot: Path) -> dict[str, str]:
    return dict(ALIAS.findall((repositoryRoot / "contracts/Models/RoleSet.cs").read_text()))


def readDeclarations(repositoryRoot: Path) -> dict[str, tuple[str, str]]:
    declared: dict[str, tuple[str, str]] = {}
    for path in source.roleVocabularyFiles(repositoryRoot):
        for owner, body in csharp.types(path.read_text(errors="replace")):
            for name, expression in DECLARATION.findall(masking.mask(body)):
                declared[f"{owner}.{name}"] = (
                    expression.strip(), path.relative_to(repositoryRoot).as_posix())
    return declared


def resolve(expression: str, declared: dict, aliases: dict, roles: list[str],
            owner: str | None = None, seen: frozenset = frozenset()) -> set[str]:
    if EVERY_ROLE.search(expression):
        return set(roles)
    if "RoleSet.Of(" in expression or "RoleSet(" in expression:
        return {aliases.get(name, name) for name in NAMED_ROLE.findall(expression)}
    reference = expression.strip().rstrip(";").split("=")[-1].strip()
    for key in candidateKeys(reference, owner, declared):
        if key in seen:
            continue
        return resolve(declared[key][0], declared, aliases, roles,
                       key.rsplit(".", 1)[0], seen | {key})
    return set()


def candidateKeys(reference: str, owner: str | None, declared: dict) -> list[str]:
    """A reference that names its owner (`JpmsRoleSets.Administrators`) means that owner's set,
    even from inside a type that declares a member of the same name; only a bare name
    (`Readers`) is looked up on the declaring type first."""
    tail = reference.split(".")[-1]
    if reference in declared:
        return [reference]
    qualifier = reference.split(".")[-2] if "." in reference else None
    byQualifier = [key for key in declared
                   if key.split(".")[-1] == tail and qualifier
                   and key.rsplit(".", 1)[0].endswith(qualifier)]
    if byQualifier:
        return byQualifier
    if owner and f"{owner}.{tail}" in declared:
        return [f"{owner}.{tail}"]
    return [key for key in declared if key.split(".")[-1] == tail][:1]


def build(repositoryRoot: Path) -> dict:
    roles = readRoles(repositoryRoot)
    aliases = readAliases(repositoryRoot)
    declared = readDeclarations(repositoryRoot)
    sets = {
        name: {"roles": sorted(resolve(expression, declared, aliases, roles,
                                       name.rsplit(".", 1)[0])),
               "file": path, "expression": " ".join(expression.split())}
        for name, (expression, path) in declared.items()}
    return {"roles": roles, "aliases": aliases, "sets": sets}
