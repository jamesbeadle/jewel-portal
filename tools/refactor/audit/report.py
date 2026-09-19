"""Writes the audit results as audit.json and audit-report.md."""
from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

from .gate import RATCHETED_FIGURES
from .inventory.file_areas import areasTable
from .score.breakdown import breakdownTable, derivation, percentage

SUMMARY_ORDER = [
    "fileLength", "functionShape", "functionNames", "accessorNames", "duplication", "naming", "comments",
    "magicValues", "prose", "conditions", "orphans", "designPatterns", "inventory", "siteDefinition", "fileAreas",
]
KIT_VERSION_FILE = Path("tools") / "refactor" / "kit-version"


def flattenSummaries(results: dict) -> dict:
    return {name: results[name]["summary"] for name in SUMMARY_ORDER if name in results}


def kitVersion(repositoryRoot: Path) -> str:
    versionPath = repositoryRoot / KIT_VERSION_FILE
    return versionPath.read_text().strip() if versionPath.exists() else ""


def writeJson(results: dict, score: dict, repositoryRoot: Path, outputDirectory: Path) -> Path:
    payload = {
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "kitVersion": kitVersion(repositoryRoot),
        "score": score,
        "summaries": flattenSummaries(results),
        "details": results,
    }
    outputPath = outputDirectory / "audit.json"
    outputPath.write_text(json.dumps(payload, indent=2))
    return outputPath


def headline(results: dict, score: dict) -> str:
    fileLength = results["fileLength"]["summary"]
    overLimit, total = fileLength["filesOverLimit"], fileLength["totalFiles"]
    share = 100 * overLimit / max(total, 1)
    return (
        f"**Code quality score {percentage(score['overall'])}.** "
        f"**{overLimit:,} of {total:,} source files are over the {fileLength['limit']}-line limit "
        f"({share:.1f}%)**; the worst file is {fileLength['worstFileLines']:,} lines."
    )


def summaryTable(results: dict) -> str:
    rows = ["| Check | Key figures |", "| --- | --- |"]
    for name, summary in flattenSummaries(results).items():
        figures = ", ".join(f"{key}: {value}" for key, value in summary.items())
        rows.append(f"| {name} | {figures} |")
    return "\n".join(rows)


def baselineTable(results: dict, score: dict, baselinePath: Path) -> str:
    if not baselinePath.exists():
        return "No `baseline.json` beside the audit — nothing to ratchet against."
    baselineAudit = json.loads(baselinePath.read_text())
    baseline, current = baselineAudit["summaries"], flattenSummaries(results)
    scoreBefore = baselineAudit.get("score", {}).get("overall")
    rows = ["| Ratcheted figure | Baseline | Now | Verdict |", "| --- | --- | --- | --- |"]
    rows.append(f"| code quality score | {percentage(scoreBefore)} | {percentage(score['overall'])} | — |")
    for checkName, figureName in RATCHETED_FIGURES:
        was, now = baseline.get(checkName, {}).get(figureName), current.get(checkName, {}).get(figureName)
        verdict = "—" if was is None or now is None else "worse" if now > was else "better" if now < was else "held"
        rows.append(f"| {checkName}.{figureName} | {was} | {now} | {verdict} |")
    return "\n".join(rows)


def worstFilesSection(results: dict) -> str:
    offenders = results.get("fileLength", {}).get("offenders", [])[:20]
    if not offenders:
        return "All files are within the limit."
    rows = ["| File | Lines |", "| --- | --- |"]
    rows += [f"| {offender['file']} | {offender['lines']} |" for offender in offenders]
    return "\n".join(rows)


def writeMarkdown(results: dict, score: dict, outputDirectory: Path) -> Path:
    baselinePath = outputDirectory.parent / "baseline.json"
    body = "\n\n".join([
        "# Refactor audit",
        f"Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}.",
        "## Headline", headline(results, score),
        "## Code quality score", breakdownTable(score), derivation(),
        "## The repository by area", areasTable(results["fileAreas"]),
        "## Summary", summaryTable(results),
        "## Against the baseline", baselineTable(results, score, baselinePath),
        "## Worst files by length", worstFilesSection(results),
        "Full detail, including every offender list, is in `audit.json`.",
    ])
    outputPath = outputDirectory / "audit-report.md"
    outputPath.write_text(body + "\n")
    return outputPath
