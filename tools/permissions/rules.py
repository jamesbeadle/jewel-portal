"""The rules a permission model has to keep, checked against what the code actually does."""
from __future__ import annotations

import re

WRITE_VERBS = {"post", "put", "patch", "delete"}
CONSULTS_A_SCOPE = re.compile(r"\w*Scope\.|Owns\w+Async|ClientProjects|OwnClientId|OwnSubcontractorId")

EVERY_ENDPOINT_IS_GATED = "every endpoint is gated"
EXTERNAL_WRITES_ARE_SCOPED = "an external write is scoped to its own rows"
CORRESPONDENCE_IS_DECLARED = "correspondence reaches only the declared roles"
CONNECTOR_MATCHES_THE_GATE = "the connector offers no more than the gate allows"
A_PAGE_STATES_WHO_MAY_OPEN_IT = "a page states who may open it"


def startsWithAny(route: str, prefixes: list[str]) -> bool:
    return any(route == prefix or route.startswith(prefix) for prefix in prefixes)


def finding(rule: str, route, verbs, detail: str, file: str) -> dict:
    return {"rule": rule, "route": route, "verbs": verbs, "detail": detail, "file": file}


def ungated(inventory: dict, policy: dict):
    excused = set(policy["openRoutes"]) | set(policy["dynamicGates"])
    for endpoint in inventory["endpoints"]:
        route = endpoint["route"] or ""
        if endpoint["roles"] or route in excused:
            continue
        if startsWithAny(route, policy["callerIsTheScope"]):
            continue
        reason = "signed in is the only check" if endpoint["authenticated"] else "no sign-in"
        yield finding(EVERY_ENDPOINT_IS_GATED, route, endpoint["verbs"], reason, endpoint["file"])


def unscopedExternalWrites(inventory: dict, policy: dict, bodies: dict):
    surfaces = policy["externalSurfaces"]
    openRoutes = set(policy["openRoutes"])
    for endpoint in inventory["endpoints"]:
        route = endpoint["route"] or ""
        roles = {role for role in set(endpoint["roles"]) & set(surfaces)
                 if not startsWithAny(route, surfaces[role])}
        if not roles or route in openRoutes or not set(endpoint["verbs"]) & WRITE_VERBS:
            continue
        if CONSULTS_A_SCOPE.search(bodies.get(endpoint["file"], "")):
            continue
        yield finding(EXTERNAL_WRITES_ARE_SCOPED, route, endpoint["verbs"],
                      f"{', '.join(sorted(roles))} may write any row, by id", endpoint["file"])


def correspondence(inventory: dict, policy: dict):
    declared = set(policy["correspondence"]["mayReach"])
    pattern = re.compile(policy["correspondence"]["routePattern"], re.I)
    for endpoint in inventory["endpoints"]:
        route = endpoint["route"] or ""
        if not pattern.search(route) or not endpoint["roles"]:
            continue
        beyond = sorted(set(endpoint["roles"]) - declared)
        if beyond:
            yield finding(CORRESPONDENCE_IS_DECLARED, route, endpoint["verbs"],
                          "also reachable by " + ", ".join(beyond), endpoint["file"])


def externalReach(inventory: dict, policy: dict):
    """An external role reaches its own surface, plus the routes the policy names for it. The
    named routes are the declaration of what is deliberately shared — the rule exists so that a
    NEW one cannot appear without someone writing it down and saying why."""
    declared = policy["externalRouteExceptions"]
    for role, prefixes in policy["externalSurfaces"].items():
        allowed = {route for group in declared.get(role, []) for route in group["routes"]}
        for endpoint in inventory["endpoints"]:
            route = endpoint["route"] or ""
            if role not in endpoint["roles"] or startsWithAny(route, prefixes) or route in allowed:
                continue
            yield finding(f"{role} reaches only what the policy declares", route, endpoint["verbs"],
                          f"admits {role}, undeclared", endpoint["file"])


def connectorDrift(actions: list[dict]):
    for action in actions:
        if not (action["visibleRoles"] and action["authorisedRoles"]):
            continue
        offered = set(action["visibleRoles"]) - set(action["authorisedRoles"])
        if offered:
            yield finding(CONNECTOR_MATCHES_THE_GATE, action["name"], ["action"],
                          "offered to " + ", ".join(sorted(offered)), action["file"])


def pagesWithoutACheck(pages: list[dict], policy: dict):
    excused = set(policy["pagesWithoutARoleCheck"])
    for page in pages:
        if page["canAccess"] or page["routes"][0] in excused:
            continue
        yield finding(A_PAGE_STATES_WHO_MAY_OPEN_IT, page["routes"][0], ["page"],
                      "no CanAccess — any approved sign-in renders it", page["file"])
