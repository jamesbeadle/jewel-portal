"""Runs every audit check and writes the report.

Usage: python -m audit.run_audit [repositoryRoot] [--output outputDirectory]
Run from tools/refactor/ (or pass paths explicitly). Requires Python 3.10+.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from . import report
from .checks import comments, duplication, file_length, function_names, function_shape, magic_values, naming, prose
from .inventory import pages_and_widgets
from .source_files import loadRules, resolveSourceFiles


def parseArguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit a repository against rules.json.")
    parser.add_argument("repositoryRoot", nargs="?", default="../..")
    parser.add_argument("--output", default="audit-output")
    return parser.parse_args()


def runChecks(repositoryRoot: Path, rules: dict) -> dict:
    sourceFiles = resolveSourceFiles(repositoryRoot, rules)
    fileChecks = [file_length, function_shape, function_names, naming, comments, magic_values, prose, pages_and_widgets]
    results = {}
    for module in fileChecks:
        result = module.check(sourceFiles, rules)
        results[result["name"]] = result
    duplicationResult = duplication.check(repositoryRoot, rules)
    results[duplicationResult["name"]] = duplicationResult
    return results


def main() -> int:
    arguments = parseArguments()
    repositoryRoot = Path(arguments.repositoryRoot).resolve()
    rules = loadRules(repositoryRoot)
    results = runChecks(repositoryRoot, rules)
    outputDirectory = Path(arguments.output)
    outputDirectory.mkdir(parents=True, exist_ok=True)
    jsonPath = report.writeJson(results, outputDirectory)
    markdownPath = report.writeMarkdown(results, outputDirectory)
    print(f"Wrote {jsonPath} and {markdownPath}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
