"""Writes the site definition as a document: every route as a user sees it, every component of the site, and the markup written by hand.

Usage, from the repository root, after run_audit: python3 -m tools.refactor.audit.site.site_document [repositoryRoot]
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from ..source_files import loadRules
from .site_definition import NAME
from .site_tables import componentTable, handRolledTable

REFACTOR_DIRECTORY = Path("tools") / "refactor"
DOCUMENT_WHEN_UNSET = "tools/refactor/site-definition.md"
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
        f"Written by the code quality check on {generatedAt[:10]} from the views themselves — never by hand, so it is never stale. "
        "Each route is what a user sees there, in the widget notation; each component of the site is defined the same way.",
        summaryLine(site["summary"]),
        f"**Notation.** {KEY}",
        "## Written by hand where a widget should be",
        handRolledTable(site["offenders"]["byWidget"]),
        "## Routes",
        *[routeSection(row) for row in site["routes"]],
        "## Components of the site",
        componentTable(site["components"]),
    ]) + "\n"


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
    print(f"Wrote {path.relative_to(repositoryRoot)}." if path else "The site definition is not measured: fill siteDefinition.catalogue in rules.json.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
