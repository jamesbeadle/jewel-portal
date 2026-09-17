"""What the score is made of: each element's figures, what they are measured against, where it reaches zero, its weight.

An element scores 100% at no offenders and falls in a straight line to 0% when offenders per unit reach
zeroAt. rules.json may override any weight or zeroAt under score.elements.<key>.
"""
from __future__ import annotations

BASELINE, PATTERNS, PROSE = "Standard baseline checks", "Design pattern file count", "Prose"
ONE = [("constant", "one")]
FILES = [("fileLength", "totalFiles")]
FUNCTIONS = [("functionShape", "totalFunctions")]
BRANCHES = [("functionShape", "ifBlocks")]
THOUSAND_LINES = [("fileLength", "totalLines")]
ORPHAN_CANDIDATES = [("inventory", "components"), ("orphans", "functionsExamined")]
MAGIC_VALUES = [("magicValues", "inlineHexColours"), ("magicValues", "inlineStyleAttributes"), ("magicValues", "repeatedStringLiterals")]


def element(key: str, label: str, group: str, figures: list, per: list, unit: str, zeroAt: float, weight: int) -> dict:
    return {"key": key, "label": label, "group": group, "figures": figures, "per": per, "unit": unit, "zeroAt": zeroAt, "weight": weight}


ELEMENTS = [
    element("filesOverLimit", "Files over the line limit", BASELINE, [("fileLength", "filesOverLimit")], FILES, "files", 0.5, 10),
    element("worstFile", "Worst file, in limits over", BASELINE, [("fileLength", "worstFileTimesOverLimit")], ONE, "", 9, 5),
    element("functionsOverLimit", "Functions over the line limit", BASELINE, [("functionShape", "functionsOverLimit")], FUNCTIONS, "functions", 0.25, 8),
    element("elseBlocks", "Else blocks", BASELINE, [("functionShape", "elseBlocks")], BRANCHES, "branches", 0.5, 5),
    element("duplication", "Duplication %", BASELINE, [("duplication", "duplicatedPercentage")], ONE, "", 20, 8),
    element("comments", "Explanatory comment lines", BASELINE, [("comments", "explanatoryCommentLines")], THOUSAND_LINES, "thousand lines", 50, 4),
    element("magicValues", "Inline magic values", BASELINE, MAGIC_VALUES, THOUSAND_LINES, "thousand lines", 20, 4),
    element("orphans", "Orphan components and functions", BASELINE,
            [("inventory", "orphanComponents"), ("orphans", "orphanFunctions")], ORPHAN_CANDIDATES, "components and functions", 0.1, 4),
    element("memberChains", "Long member chain lines", BASELINE, [("prose", "longMemberChainLines")], THOUSAND_LINES, "thousand lines", 30, 4),
    element("deepIndentation", "Deeply indented lines", BASELINE, [("prose", "deeplyIndentedLines")], THOUSAND_LINES, "thousand lines", 30, 4),
    element("functionNames", "Overlong function names", BASELINE, [("functionNames", "overlongFunctionNames")], FUNCTIONS, "functions", 0.1, 4),
    element("predictedFiles", "Files the patterns predict but are missing", PATTERNS,
            [("designPatterns", "predictedFilesMissing")], [("designPatterns", "predictedFiles")], "predicted files", 0.5, 10),
    element("entityFileCounts", "Entities outside their expected file count", PATTERNS,
            [("designPatterns", "entitiesOutOfRange")], [("designPatterns", "entities")], "entities", 0.5, 10),
    element("tangledConditions", "Conditions with calls tangled inside calls", PROSE, [("conditions", "tangledConditionLines")], BRANCHES, "branches", 0.25, 8),
    element("literalComparisons", "Conditions compared to a raw literal", PROSE, [("conditions", "literalComparisonLines")], BRANCHES, "branches", 0.25, 6),
    element("gluedAccessors", "Accessor names that want to be a property", PROSE, [("accessorNames", "gluedAccessorNames")], FUNCTIONS, "functions", 0.1, 6),
]


def elementsFor(rules: dict) -> list[dict]:
    overrides = rules.get("score", {}).get("elements", {})
    return [{**row, **overrides.get(row["key"], {})} for row in ELEMENTS]
