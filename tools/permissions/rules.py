"""The rules a permission model has to keep, checked against what the code actually does."""
from __future__ import annotations

import re

WRITE_VERBS = {"post", "put", "patch", "delete"}
CONSULTS_A_SCOPE = re.compile(r"\w*Scope\.|Owns\w+Async|ClientProjects|OwnClientId|OwnSubcontractorId")

EVERY_ENDPOINT_IS_GATED = "every endpoint is gated"
EXTERNAL_WRITES_ARE_SCOPED = "an external write is scoped to its own rows"
CORRESPONDENCE_IS_READ_BY = "correspondence is read by the internal team"
CORRESPONDENCE_IS_SENT_BY = "correspondence is sent by the directors"
MAIL_IS_REACHED_BY_THE_INTERNAL_TEAM = "mail is reached only by the internal team"
CONNECTOR_MATCHES_THE_GATE = "the connector offers no more than the gate allows"
A_PAGE_STATES_WHO_MAY_OPEN_IT = "a page states who may open it"
A_SCOPED_COMMAND_IS_SCOPED_ON_THE_CONNECTOR = "a scoped command is scoped on the connector too"
COMMAND_HANDLED = re.compile(r"ICommandHandler<(\w+),")
COMMAND_DISPATCHED = re.compile(r"typeof\((\w+)\)|new (\w+)\(")


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


CORRESPONDENCE_RULES = {"read": CORRESPONDENCE_IS_READ_BY, "send": CORRESPONDENCE_IS_SENT_BY}


def correspondence(inventory: dict, policy: dict):
    """Reading correspondence and sending it are two different permissions, so they are two rules.
    A route that both reads and sends is judged as a send, which is the stricter of the two."""
    for kind in ("send", "read"):
        declared = set(policy["correspondence"][kind]["mayReach"])
        pattern = re.compile(policy["correspondence"][kind]["routePattern"], re.I)
        sending = re.compile(policy["correspondence"]["send"]["routePattern"], re.I)
        for endpoint in inventory["endpoints"]:
            route = endpoint["route"] or ""
            if not pattern.search(route) or not endpoint["roles"]:
                continue
            if kind == "read" and sending.search(route):
                continue
            beyond = sorted(set(endpoint["roles"]) - declared)
            if beyond:
                yield finding(CORRESPONDENCE_RULES[kind], route, endpoint["verbs"],
                              "also reachable by " + ", ".join(beyond), endpoint["file"])


def mailReached(inventory: dict, policy: dict, endpointReader, toolReader):
    """The route never has to say mail: an endpoint or connector tool that reaches a mail reader
    is correspondence, whatever it is called, and admits the internal team alone. The two readers
    answer which mail reader an endpoint type (with its verbs) or a tool's body reaches, or None."""
    declared = set(policy["correspondence"]["read"]["mayReach"])
    for endpoint in inventory["endpoints"]:
        reader = endpointReader(endpoint["type"], endpoint["verbs"])
        if not reader:
            continue
        beyond = sorted(set(endpoint["roles"]) - declared)
        if beyond or not endpoint["roles"]:
            detail = "also reachable by " + ", ".join(beyond) if beyond else "no role gate"
            yield finding(MAIL_IS_REACHED_BY_THE_INTERNAL_TEAM, endpoint["route"], endpoint["verbs"],
                          f"reaches {reader}; {detail}", endpoint["file"])
    for tool in inventory["connectorTools"]:
        reader = toolReader(tool["body"])
        beyond = sorted(set(tool["roles"]) - declared)
        if reader and beyond:
            yield finding(MAIL_IS_REACHED_BY_THE_INTERNAL_TEAM, tool["name"], ["tool"],
                          f"reaches {reader}; also offered to " + ", ".join(beyond), tool["file"])


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
    """A page says who may open it by naming a role set (Page OpenTo) — the same constant its
    API reads are gated by. CanAccess is a page's further check on top, never the role answer."""
    excused = set(policy["pagesWithoutARoleCheck"])
    for page in pages:
        if page["openTo"] or page["routes"][0] in excused:
            continue
        yield finding(A_PAGE_STATES_WHO_MAY_OPEN_IT, page["routes"][0], ["page"],
                      "no OpenTo — any approved sign-in renders it", page["file"])


def scopedCommandsOffTheConnector(inventory: dict, bodies: dict, connectorText: str, scopesText: str):
    """An endpoint that consults a record scope after its role gate is matched by the connector,
    which runs the same command through AiActionScopes; a command scoped on the site and
    dispatched by the connector without an entry there reaches further over MCP than on the site."""
    dispatched = {name for pair in COMMAND_DISPATCHED.findall(connectorText) for name in pair if name}
    reported: set[str] = set()
    for endpoint in inventory["endpoints"]:
        body = bodies.get(endpoint["file"], "")
        if not CONSULTS_A_SCOPE.search(body):
            continue
        for command in COMMAND_HANDLED.findall(body):
            if command in reported or command not in dispatched or f"typeof({command})" in scopesText:
                continue
            reported.add(command)
            yield finding(A_SCOPED_COMMAND_IS_SCOPED_ON_THE_CONNECTOR, endpoint["route"], endpoint["verbs"],
                          f"{command} consults a scope on the site and none in AiActionScopes", endpoint["file"])
