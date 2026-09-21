"""The UI surface: every routed page and the role check it makes before it renders."""
from __future__ import annotations

import re
from pathlib import Path

from . import source

PAGE_ROUTE = re.compile(r'@page\s+"([^"]+)"')
OPEN_TO = re.compile(r'OpenTo\s*=\s*"@?\(?([^"]+?)\)?"')
CAN_ACCESS = re.compile(r'CanAccess\s*=\s*"@?\(?([^"]+?)\)?"')
NAMED_ROLE = re.compile(r"Role\.(\w+)")


def attribute(pattern: re.Pattern, text: str) -> str | None:
    match = pattern.search(text)
    return " ".join(match.group(1).split()) if match else None


def collect(repositoryRoot: Path) -> list[dict]:
    pages: list[dict] = []
    for path in source.viewFiles(repositoryRoot):
        text = path.read_text(errors="replace")
        routes = PAGE_ROUTE.findall(text)
        if not routes:
            continue
        codeBehind = path.with_suffix(".razor.cs")
        behind = codeBehind.read_text(errors="replace") if codeBehind.exists() else ""
        pages.append({
            "routes": routes,
            "file": path.relative_to(repositoryRoot).as_posix(),
            "openTo": attribute(OPEN_TO, text),
            "canAccess": attribute(CAN_ACCESS, text),
            "rolesNamed": sorted(set(NAMED_ROLE.findall(text + behind))),
        })
    return pages
