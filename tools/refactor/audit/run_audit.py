"""Runs every audit check, scores the result and writes the report.

Usage: python3 -m tools.refactor.audit.run_audit [repositoryRoot] [--output outputDirectory] [--fast]
--fast carries the duplication figure forward from the baseline instead of measuring it again, for the
readings taken between the steps of a round; the first and last readings of a round are always full.
Requires Python 3.10+.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from . import report
from .checks import (
    accessor_names, comments, conditions, design_patterns, duplication, file_length,
    function_names, function_shape, magic_values, naming, orphans, prose,
)
from .inventory import file_areas, pages_and_widgets
from .score.calculate import calculate
from .source_files import loadRules, resolveSourceFiles

FILE_CHECKS = [
    file_length, function_shape, function_names, accessor_names, naming, comments, magic_values,
    prose, conditions, orphans, design_patterns, pages_and_widgets,
]
NOT_MEASURED = {"name": "duplication", "summary": {"skipped": "--fast reading with no baseline to carry from"}, "offenders": []}


def parseArguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit a repository against rules.json.")
    parser.add_argument("repositoryRoot", nargs="?", default="../..")
    parser.add_argument("--output", default="audit-output")
    parser.add_argument("--fast", action="store_true")
    return parser.parse_args()


def duplicationFromBaseline(baselinePath: Path) -> dict:
    if not baselinePath.exists():
        return NOT_MEASURED
    carried = json.loads(baselinePath.read_text())["details"]["duplication"]
    return {**carried, "summary": {**carried["summary"], "carriedFromBaseline": True}}


def runChecks(repositoryRoot: Path, rules: dict, baselinePath: Path | None = None) -> dict:
    sourceFiles = resolveSourceFiles(repositoryRoot, rules)
    results = {}
    for module in FILE_CHECKS:
        result = module.check(sourceFiles, rules)
        results[result["name"]] = result
    results["fileAreas"] = file_areas.check(repositoryRoot, sourceFiles, rules)
    results["duplication"] = duplicationFromBaseline(baselinePath) if baselinePath else duplication.check(repositoryRoot, rules)
    return results


def audit(repositoryRoot: Path, outputDirectory: Path, isFast: bool = False) -> dict:
    rules = loadRules(repositoryRoot)
    results = runChecks(repositoryRoot, rules, outputDirectory.parent / "baseline.json" if isFast else None)
    score = calculate(report.flattenSummaries(results), rules)
    outputDirectory.mkdir(parents=True, exist_ok=True)
    jsonPath = report.writeJson(results, score, repositoryRoot, outputDirectory)
    markdownPath = report.writeMarkdown(results, score, outputDirectory)
    print(f"Code quality score {score['overall']}% — wrote {jsonPath} and {markdownPath}")
    return score


def main() -> int:
    arguments = parseArguments()
    audit(Path(arguments.repositoryRoot).resolve(), Path(arguments.output), arguments.fast)
    return 0


if __name__ == "__main__":
    sys.exit(main())
