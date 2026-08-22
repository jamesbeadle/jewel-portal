"""Measures duplication via jscpd when installed; degrades gracefully when not."""
from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
from collections import Counter
from pathlib import Path


def runJscpd(repositoryRoot: Path, rules: dict, reportDirectory: Path) -> Path | None:
    if shutil.which("jscpd") is None:
        return None
    duplicationRules = rules["duplication"]
    command = [
        "jscpd",
        "--pattern", "{" + ",".join(rules["sourceGlobs"]) + "}",
        "--ignore", ",".join(duplicationRules["ignoreGlobs"]),
        "--min-tokens", str(duplicationRules["minTokens"]),
        "--reporters", "json",
        "--output", str(reportDirectory),
        "--silent",
    ]
    subprocess.run(command, cwd=repositoryRoot, capture_output=True)
    reportPath = reportDirectory / "jscpd-report.json"
    return reportPath if reportPath.exists() else None


def summariseReport(reportPath: Path, repositoryRoot: Path) -> dict:
    report = json.loads(reportPath.read_text())
    total = report["statistics"]["total"]
    hotspots: Counter[str] = Counter()
    for duplicate in report["duplicates"]:
        for side in ("firstFile", "secondFile"):
            relative = Path(duplicate[side]["name"]).as_posix().replace(
                repositoryRoot.as_posix() + "/", ""
            )
            hotspots[relative] += duplicate["lines"]
    return {
        "clones": total["clones"],
        "duplicatedLines": total["duplicatedLines"],
        "totalLines": total["lines"],
        "duplicatedPercentage": round(total["percentage"], 2),
        "hotspots": [
            {"file": file, "duplicatedLineInstances": count}
            for file, count in hotspots.most_common(15)
        ],
    }


def check(repositoryRoot: Path, rules: dict) -> dict:
    with tempfile.TemporaryDirectory() as temporaryDirectory:
        reportPath = runJscpd(repositoryRoot, rules, Path(temporaryDirectory))
        if reportPath is None:
            return {
                "name": "duplication",
                "summary": {"skipped": "jscpd is not installed (npm install -g jscpd)"},
                "offenders": [],
            }
        summary = summariseReport(reportPath, repositoryRoot)
    return {
        "name": "duplication",
        "summary": {key: value for key, value in summary.items() if key != "hotspots"},
        "offenders": summary["hotspots"],
    }
