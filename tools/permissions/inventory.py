"""The permission model as the code actually declares it, across all three surfaces."""
from __future__ import annotations

from pathlib import Path

from . import connector, endpoints, gates, views, vocabulary


def build(repositoryRoot: Path) -> dict:
    vocabularyModel = vocabulary.build(repositoryRoot)
    byName = endpoints.declarations(repositoryRoot)
    resolver = gates.Resolver(vocabularyModel["sets"], vocabularyModel["roles"],
                              endpoints.mergedBodies(byName))
    return {
        "roles": vocabularyModel["roles"],
        "roleSets": vocabularyModel["sets"],
        "endpoints": endpoints.collect(repositoryRoot, resolver, byName),
        "pages": views.collect(repositoryRoot),
        "connectorTools": connector.tools(repositoryRoot, resolver),
        "connectorActions": connector.actions(repositoryRoot, resolver),
    }


def reachByRole(inventory: dict) -> dict[str, int]:
    """How many gated endpoints each role admits. Role.Admin expands to every role at resolution
    (UserRoles.Expand), so it is counted against the whole gated set rather than the gates that
    happen to name it."""
    gated = [endpoint for endpoint in inventory["endpoints"] if endpoint["roles"]]
    counts = {role: sum(1 for endpoint in gated if role in endpoint["roles"])
              for role in inventory["roles"]}
    counts["Admin"] = len(gated)
    return counts
