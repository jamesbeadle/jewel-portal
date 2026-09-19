"""Writes the site definition as a document: every route as a user sees it, every component of the site, and the markup written by hand.

Usage, from the repository root, after run_audit: python3 -m tools.refactor.audit.site.site_document [repositoryRoot]
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from ..source_files import loadRules
from .site_definition import NAME
from .site_designs import NEVER_CHECKED
from .site_tables import componentTable, designTable, handRolledTable

REFACTOR_DIRECTORY = Path("tools") / "refactor"
DOCUMENT_WHEN_UNSET = "tools/refactor/site-definition.md"
NOT_MEASURED = "The site definition is not measured: name the shared widgets in rules.json under siteDefinition.catalogue, then read again."
KEY = (
    "`a, b` stacked top to bottom · `[ a b ]` side by side · `(3) a` three of them, `(n) a` one per item · "
    "`?when: a` shown on a condition · `( a | b )` one or the other · `Widget{ … }` a catalogue widget with its content · "
    "`Part=( … )` one of the site's own components opened out · `⚠table→RecordsTable` markup written by hand where "
    "the named widget should be."
)


def summaryLine(summary: dict) -> str:
    return (
        f"**{summary['routes']} routes, {summary['siteComponents']} components of the site, {summary['catalogue']} catalogue widgets.** "
        f"{summary['widgetUsages']:,} widget usages and {summary['handRolledElements']:,} pieces of markup written by hand where a "
        f"widget should be, in {summary['viewsWithHandRolled']} views."
    )


def routeSection(row: dict) -> str:
    return f"### `{row['route']}`\n`{row['file']}`\n\n```\n{row['expanded']}\n```"


def renderDocument(site: dict, generatedAt: str) -> str:
    return "\n\n".join([
        "# Site definition",
        f"Written on {generatedAt[:10]} from the views themselves — by the widget identification or the code quality check, never by hand, so it is never stale. "
        "Each route is what a user sees there, in the widget notation; each component of the site is defined the same way.",
        summaryLine(site["summary"]),
        f"**Notation.** {KEY}",
        "## Written by hand where a widget should be",
        handRolledTable(site["offenders"]["byWidget"]),
        "## The widgets' designs",
        designsStanding(site.get("designs", {})),
        designTable(site.get("designs", {})),
        "## Routes",
        *[routeSection(row) for row in site["routes"]],
        "## Components of the site",
        componentTable(site["components"]),
    ]) + "\n"


def designsStanding(widgetDesigns: dict) -> str:
    if not widgetDesigns.get("isSetUp"):
        return (
            f"No design index yet (`{widgetDesigns.get('index', '')}`). Say *\"Set up the widget designs\"* to create it, "
            "*\"Extract the brand from <references>\"* for the brand sheet, then *\"Extract the design for <Widget> from <images>\"* per widget "
            "(the `widget-design` skill)."
        )
    return (
        f"Brand sheet: `{widgetDesigns.get('brand') or '—'}`, the site last checked against it {whenChecked(widgetDesigns['brandCheckedAt'])}. "
        f"{widgetDesigns['sheets']} of {len(widgetDesigns['widgets'])} catalogue widgets have a design sheet; the widgets were last checked "
        f"against them {whenChecked(widgetDesigns['lastChecked'])}. Say *\"Check the site against the brand\"* and "
        "*\"Check the widgets against their designs\"* to run the checks, *\"Extract the brand from <references>\"* and "
        "*\"Extract the design for <Widget> from <images>\"* to bring the sheets up to date (the `widget-design` skill) — each a "
        "judgement, so run on request, never by the audit."
    )


def whenChecked(checked: str) -> str:
    return "never" if checked == NEVER_CHECKED else f"on {checked}"


def documentPath(repositoryRoot: Path) -> Path:
    settings = loadRules(repositoryRoot).get("siteDefinition", {})
    return repositoryRoot / settings.get("document", DOCUMENT_WHEN_UNSET)


def writeDocument(repositoryRoot: Path, audit: dict) -> Path | None:
    site = audit["details"].get(NAME, {})
    if "routes" not in site:
        return None
    path = documentPath(repositoryRoot)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(renderDocument(site, audit["generatedAt"]))
    return path


def main() -> int:
    repositoryRoot = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    audit = json.loads((repositoryRoot / REFACTOR_DIRECTORY / "audit-output" / "audit.json").read_text())
    path = writeDocument(repositoryRoot, audit)
    print(f"Wrote {path.relative_to(repositoryRoot)}." if path else NOT_MEASURED)
    return 0


if __name__ == "__main__":
    sys.exit(main())
