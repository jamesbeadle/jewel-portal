"""Writes the audit results as audit.json and audit-report.md."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

SUMMARY_ORDER = [
    "fileLength", "functionShape", "functionNames", "duplication", "naming",
    "comments", "magicValues", "prose", "inventory",
]


def flattenSummaries(results: dict) -> dict:
    return {name: results[name]["summary"] for name in SUMMARY_ORDER if name in results}


def writeJson(results: dict, outputDirectory: Path) -> Path:
    payload = {
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "summaries": flattenSummaries(results),
        "details": results,
    }
    outputPath = outputDirectory / "audit.json"
    outputPath.write_text(json.dumps(payload, indent=2))
    return outputPath


def summaryTable(results: dict) -> str:
    rows = ["| Check | Key figures |", "| --- | --- |"]
    for name, summary in flattenSummaries(results).items():
        figures = ", ".join(f"{key}: {value}" for key, value in summary.items())
        rows.append(f"| {name} | {figures} |")
    return "\n".join(rows)


def worstFilesSection(results: dict) -> str:
    offenders = results.get("fileLength", {}).get("offenders", [])[:20]
    if not offenders:
        return "All files are within the limit."
    rows = ["| File | Lines |", "| --- | --- |"]
    rows += [f"| {offender['file']} | {offender['lines']} |" for offender in offenders]
    return "\n".join(rows)


def writeMarkdown(results: dict, outputDirectory: Path) -> Path:
    body = "\n\n".join([
        "# Refactor audit",
        f"Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}.",
        "## Summary", summaryTable(results),
        "## Worst files by length", worstFilesSection(results),
        "Full detail, including every offender list, is in `audit.json`.",
    ])
    outputPath = outputDirectory / "audit-report.md"
    outputPath.write_text(body + "\n")
    return outputPath
