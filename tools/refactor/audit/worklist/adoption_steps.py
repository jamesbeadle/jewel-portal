"""The widget adoption pass of the plan: file-sized steps, each adopting one widget in a few files where its markup is written by hand."""
from __future__ import annotations

from collections import Counter

ADOPTION = "Pass 1 — Widget adoption"
FILES_PER_STEP = 4


def countsByFile(findings: list[dict], widget: str) -> list[tuple[str, int]]:
    counts = Counter(row["file"] for row in findings if row["ownedBy"] == widget)
    return sorted(counts.items(), key=lambda item: (-item[1], item[0]))


def batches(files: list[tuple[str, int]]) -> list[list[tuple[str, int]]]:
    return [files[start : start + FILES_PER_STEP] for start in range(0, len(files), FILES_PER_STEP)]


def describeFiles(batch: list[tuple[str, int]]) -> str:
    return ", ".join(f"`{file}` ({count})" if count > 1 else f"`{file}`" for file, count in batch)


def adoptionStep(widget: str, batch: list[tuple[str, int]], isBuilt: bool, index: int) -> dict:
    places = sum(count for _, count in batch)
    verb = "Adopt" if isBuilt or index > 0 else "Build, from the best of these, then adopt"
    return {
        "pass": ADOPTION,
        "order": (-places, widget, index),
        "title": f"{verb} `{widget}` in {len(batch)} file(s) that write its markup by hand",
        "detail": (
            f"{places} place(s): {describeFiles(batch)}. Each is listed with its line in audit.json under "
            "details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md."
        ),
    }


def widgetSteps(widget: str, findings: list[dict], isBuilt: bool) -> list[dict]:
    return [
        adoptionStep(widget, batch, isBuilt, index)
        for index, batch in enumerate(batches(countsByFile(findings, widget)))
    ]


def adoptionSteps(audit: dict) -> list[dict]:
    site = audit["details"].get("siteDefinition", {})
    findings = site.get("offenders", {}).get("handRolled", [])
    catalogue = set(site.get("catalogueNames", []))
    widgets = [row["ownedBy"] for row in site.get("offenders", {}).get("byWidget", [])]
    steps = [step for widget in widgets for step in widgetSteps(widget, findings, widget in catalogue)]
    steps.sort(key=lambda step: step["order"])
    return [{key: value for key, value in step.items() if key != "order"} for step in steps]
