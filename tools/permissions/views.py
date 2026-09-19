"""The UI surface: every routed page and the role check it makes before it renders."""
from __future__ import annotations

import re
from pathlib import Path

from . import source

PAGE_ROUTE = re.compile(r'@page\s+"([^"]+)"')
CAN_ACCESS = re.compile(r'CanAccess\s*=\s*"@?\(?([^"]+?)\)?"')
NAMED_ROLE = re.compile(r"Role\.(\w+)")


def accessCheck(text: str) -> str | None:
    match = CAN_ACCESS.search(text)
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
            "canAccess": accessCheck(text),
            "rolesNamed": sorted(set(NAMED_ROLE.findall(text + behind))),
        })
    return pages
