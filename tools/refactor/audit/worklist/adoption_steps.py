"""The widget adoption pass of the plan: one step per widget, for every place its markup is still written by hand."""
from __future__ import annotations

ADOPTION = "Pass 2 — Widget adoption"
SHOWN_FILES = 4


def adoptionStep(row: dict, catalogue: set[str]) -> dict:
    files = ", ".join(row["files"][:SHOWN_FILES])
    extra = f" +{len(row['files']) - SHOWN_FILES}" if len(row["files"]) > SHOWN_FILES else ""
    isBuilt = row["ownedBy"] in catalogue
    verb = "Adopt" if isBuilt else "Build, then adopt,"
    return {
        "pass": ADOPTION,
        "title": f"{verb} `{row['ownedBy']}` where its markup is written by hand",
        "detail": (
            f"{row['count']} places in {len(row['files'])} files: {files}{extra}. Each is listed with its line in audit.json under "
            "details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md."
        ),
    }


def adoptionSteps(audit: dict) -> list[dict]:
    site = audit["details"].get("siteDefinition", {})
    byWidget = site.get("offenders", {}).get("byWidget", [])
    catalogue = {row["ownedBy"] for row in byWidget if row["ownedBy"] in site.get("catalogueNames", [])}
    return [adoptionStep(row, catalogue) for row in byWidget]
