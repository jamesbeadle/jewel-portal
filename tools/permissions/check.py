"""The system-wide permission check.

Usage: python3 -m tools.permissions.check . [--json tools/permissions/permission-check.json]

Reads the permission model out of the API, the UI and the MCP connector, and reports every place
it disagrees with tools/permissions/policy.json. Read-only; it writes no source file."""
from __future__ import annotations

import json
import sys
from pathlib import Path

from . import connector, inventory, rules, source

RULES_IN_REPORT_ORDER = [
    rules.EVERY_ENDPOINT_IS_GATED,
    rules.EXTERNAL_WRITES_ARE_SCOPED,
    rules.CORRESPONDENCE_IS_READ_BY,
    rules.CORRESPONDENCE_IS_SENT_BY,
    "Client reaches only what the policy declares",
    "Subcontractor reaches only what the policy declares",
    "SiteOperative reaches only what the policy declares",
    "Architect reaches only what the policy declares",
    rules.CONNECTOR_MATCHES_THE_GATE,
    rules.A_SCOPED_COMMAND_IS_SCOPED_ON_THE_CONNECTOR,
    rules.A_PAGE_STATES_WHO_MAY_OPEN_IT,
]
FINDINGS_SHOWN_PER_RULE = 12


def readPolicy(repositoryRoot: Path, stated: str | None) -> dict:
    """The policy travels with the tool, not with the tree being read — `--policy` lets one
    checkout measure another, which is how a branch that predates the tool is measured at all."""
    here = Path(__file__).parent / "policy.json"
    chosen = Path(stated) if stated else (repositoryRoot / "tools/permissions/policy.json")
    return json.loads((chosen if chosen.exists() else here).read_text())


def findingsFor(model: dict, policy: dict, repositoryRoot: Path) -> list[dict]:
    bodies = {entry["file"]: (repositoryRoot / entry["file"]).read_text(errors="replace")
              for entry in model["endpoints"]}
    return [*rules.ungated(model, policy),
            *rules.unscopedExternalWrites(model, policy, bodies),
            *rules.correspondence(model, policy),
            *rules.externalReach(model, policy),
            *rules.connectorDrift(model["connectorActions"]),
            *rules.scopedCommandsOffTheConnector(model, bodies, connectorText(repositoryRoot),
                                                 scopesText(repositoryRoot)),
            *rules.pagesWithoutACheck(model["pages"], policy)]


def connectorText(repositoryRoot: Path) -> str:
    return "\n".join(path.read_text(errors="replace") for path in source.connectorFiles(repositoryRoot))


def scopesText(repositoryRoot: Path) -> str:
    scopes = repositoryRoot / "api/Features/Ai/Tools/Actions/AiActionScopes.cs"
    return scopes.read_text(errors="replace") if scopes.exists() else ""


def summarise(findings: list[dict]) -> dict[str, int]:
    return {rule: sum(1 for entry in findings if entry["rule"] == rule)
            for rule in RULES_IN_REPORT_ORDER}


def report(model: dict, findings: list[dict]) -> None:
    print(f"Endpoints {len(model['endpoints'])}  ·  routed pages {len(model['pages'])}  ·  "
          f"connector actions {len(model['connectorActions'])}  ·  "
          f"role sets {len(model['roleSets'])}  ·  findings {len(findings)}\n")
    for rule in RULES_IN_REPORT_ORDER:
        matching = [entry for entry in findings if entry["rule"] == rule]
        if not matching:
            print(f"PASS  {rule}")
            continue
        print(f"\nFAIL  {rule} — {len(matching)}")
        for entry in matching[:FINDINGS_SHOWN_PER_RULE]:
            verbs = "/".join(entry["verbs"])
            print(f"      {verbs:14} {str(entry['route'])[:46]:48} {entry['detail']}")
        if len(matching) > FINDINGS_SHOWN_PER_RULE:
            print(f"      … and {len(matching) - FINDINGS_SHOWN_PER_RULE} more")


def writeJson(destination: Path, model: dict, findings: list[dict]) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps({
        "summaries": summarise(findings),
        "reachByRole": inventory.reachByRole(model),
        "findings": findings,
        "endpoints": model["endpoints"],
        "pages": model["pages"],
        "connectorActions": model["connectorActions"],
    }, indent=1) + "\n")


def writeBaseline(destination: Path, model: dict, findings: list[dict]) -> None:
    """The committed baseline holds the counts the gate ratchets and the reach each role has —
    small enough to read in a pull request, which the full reading is not."""
    destination.write_text(json.dumps({
        "summaries": summarise(findings),
        "reachByRole": inventory.reachByRole(model),
    }, indent=1) + "\n")


def argumentAfter(flag: str) -> str | None:
    return sys.argv[sys.argv.index(flag) + 1] if flag in sys.argv else None


def main() -> int:
    repositoryRoot = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    model = inventory.build(repositoryRoot)
    findings = findingsFor(model, readPolicy(repositoryRoot, argumentAfter("--policy")),
                           repositoryRoot)
    report(model, findings)
    if argumentAfter("--json"):
        writeJson(Path(argumentAfter("--json")), model, findings)
    if argumentAfter("--baseline"):
        writeBaseline(Path(argumentAfter("--baseline")), model, findings)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
