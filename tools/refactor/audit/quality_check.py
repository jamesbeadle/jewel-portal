"""The code quality check, end to end: audit the repository, score it, write its refactoring plan, put the box in the README.

Usage, from the repository root: python3 -m tools.refactor.audit.quality_check [repositoryRoot]
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from .plan import writePlan
from .readme_box import writeBox
from .run_audit import audit
from .site.site_document import writeDocument

OUTPUT_DIRECTORY = Path("tools") / "refactor" / "audit-output"


def main() -> int:
    repositoryRoot = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    outputDirectory = repositoryRoot / OUTPUT_DIRECTORY
    audit(repositoryRoot, outputDirectory)
    auditPayload = json.loads((outputDirectory / "audit.json").read_text())
    plan = writePlan(repositoryRoot, auditPayload)
    writeBox(repositoryRoot / "README.md", auditPayload, plan["steps"])
    sitePath = writeDocument(repositoryRoot, auditPayload)
    print(f"The score box is at the bottom of README.md; the {len(plan['steps'])}-step plan is in tools/refactor/refactor-plan.md.")
    if sitePath:
        print(f"The site definition is in {sitePath.relative_to(repositoryRoot)}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
