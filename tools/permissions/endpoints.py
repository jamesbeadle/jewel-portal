"""Every HTTP endpoint the API exposes, and the roles its gate admits."""
from __future__ import annotations

import re
from pathlib import Path

from . import csharp, gates, source

TRIGGER = re.compile(r"\[HttpTrigger\((?P<arguments>[^\]]*?)\)\]", re.S)
VERB = re.compile(r'"(get|post|put|patch|delete|head|options)"', re.I)
ROUTE = re.compile(r'Route\s*=\s*"([^"]*)"')
FUNCTION = re.compile(r'\[Function\(\s*(?:nameof\((\w+)\)|"([^"]+)")\s*\)\]')
RESOLVES_USER = re.compile(r"\.Resolve(?:Async|ByEmailAsync)\s*\(")


def declarations(repositoryRoot: Path) -> dict[str, list[tuple[Path, str]]]:
    """Every type declared in the API, keyed by name — a partial class spread over several files
    arrives here as several declarations of one name."""
    found: dict[str, list[tuple[Path, str]]] = {}
    for path in source.apiFiles(repositoryRoot):
        for name, body in csharp.types(path.read_text(errors="replace")):
            found.setdefault(name, []).append((path, body))
    return found


def mergedBodies(byName: dict) -> dict[str, str]:
    return {name: "\n".join(body for _, body in parts) for name, parts in byName.items()}


def functionName(text: str, position: int) -> str | None:
    names = [match.group(1) or match.group(2) for match in FUNCTION.finditer(text, 0, position)]
    return names[-1] if names else None


def withGateTypesNamed(body: str, declaredFields: dict[str, str]) -> str:
    """The same body with each injected gate's TYPE spelled out, so a call through a field
    (`authorisation.Allows(...)`) is followed as readily as one through a type."""
    named = [f"{declaredFields[name]}.Allows()"
             for name, _ in gates.GATE_CALL.findall(body) if name in declaredFields]
    return body + "\n" + "\n".join(named)


def inDeclaration(typeName: str, path: Path, body: str, wholeType: str,
                  repositoryRoot: Path, resolver: gates.Resolver):
    siblings = csharp.members(wholeType)
    declaredFields = csharp.fields(wholeType)
    for trigger in TRIGGER.finditer(body):
        entry = csharp.bodyAfterParameters(body, trigger.end())
        reachable = withGateTypesNamed(csharp.reachableBody(entry, siblings), declaredFields)
        outcome = resolver.ofBody(reachable, owner=typeName)
        route = ROUTE.search(trigger.group("arguments"))
        yield {
            "route": route.group(1) if route else None,
            "verbs": sorted({verb.lower() for verb in VERB.findall(trigger.group("arguments"))}),
            "function": functionName(body, trigger.start()),
            "type": typeName,
            "file": path.relative_to(repositoryRoot).as_posix(),
            "authenticated": bool(RESOLVES_USER.search(reachable)),
            "roles": outcome["roles"],
            "gateSources": outcome["sources"],
            "unresolvedGates": outcome["unresolved"],
        }


def collect(repositoryRoot: Path, resolver: gates.Resolver, byName: dict) -> list[dict]:
    whole = mergedBodies(byName)
    seen: set[tuple] = set()
    found: list[dict] = []
    for typeName, parts in byName.items():
        for path, body in parts:
            for entry in inDeclaration(typeName, path, body, whole[typeName],
                                       repositoryRoot, resolver):
                key = (entry["file"], entry["function"], entry["route"], tuple(entry["verbs"]))
                if key not in seen:
                    seen.add(key)
                    found.append(entry)
    return sorted(found, key=lambda entry: (entry["route"] or "", entry["verbs"]))
