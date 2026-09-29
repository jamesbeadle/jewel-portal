"""The doors into the data: each entry point, the tables it writes through everything it reaches, and its checks.

A door writes a table when it, or a file it reaches, matches a write pattern naming a table the schema declares.
It is guarded when it, or a file it reaches, matches a validator pattern. A validator that names a column of the
table it speaks for and allows it more characters than that column holds is a looser limit: the check and the
schema have drifted. The table a validator speaks for is the one its own declaration names (subjects.py), never
another table the door happens to reach, so a column name shared between tables — description, name, title —
is compared with its own table's column, and the reading is the same on every run. A line that declares a
property, names a member in a lambda or keys an object names that column alone; any other line names every
identifier on it.
"""
from __future__ import annotations

import re

from ..source_files import SourceFile, matchesAny
from .reach import Reach
from .schema_columns import columnKey, tableKey
from .subjects import Subjects

LIMIT_IN_VALIDATOR = re.compile(r"(?:\bmax|maxLength|MaxLength|MaximumLength|StringLength|Length)\s*(?:\(\s*|[:=]\s*)(?:\d+\s*,\s*)?(\d+)")
NAME = re.compile(r"[A-Za-z_]\w*")
COLUMN_DECLARED = [re.compile(r"\b(\w+)\s*\{\s*get\b"), re.compile(r"=>\s*\w+\.(\w+)"),
                   re.compile(r"^\s*[\"']?(\w+)[\"']?\s*:")]


def tablesWritten(sourceFile: SourceFile, writePatterns: list, schema: dict) -> set[str]:
    text = "\n".join(sourceFile.lines)
    named = {tableKey(found.group("table")) for pattern in writePatterns for found in pattern.finditer(text)}
    return {table for table in named if table in schema}


def isValidator(sourceFile: SourceFile, validatorPatterns: list) -> bool:
    return any(pattern.search(line) for line in sourceFile.lines for pattern in validatorPatterns)


def columnsNamed(line: str) -> set[str]:
    declared = next((found for pattern in COLUMN_DECLARED if (found := pattern.search(line))), None)
    if declared:
        return {columnKey(declared.group(1))}
    return {columnKey(name) for name in NAME.findall(line)}


def limitedColumnsNamed(line: str, columns: dict) -> list[str]:
    return sorted(column for column in columnsNamed(line) if columns.get(column))


def looserLimitsIn(validator: SourceFile, tableByLine: list[str | None], written: set[str], schema: dict) -> list[dict]:
    found = []
    for number, (line, table) in enumerate(zip(validator.lines, tableByLine), start=1):
        if table not in written:
            continue
        allowed = LIMIT_IN_VALIDATOR.search(line)
        if not allowed:
            continue
        allowedLength = int(allowed.group(1))
        columns = schema[table]
        found += [{"file": validator.relative, "line": number, "table": table, "column": column,
                   "columnLimit": columns[column], "validatorLimit": allowedLength}
                  for column in limitedColumnsNamed(line, columns) if allowedLength > columns[column]]
    return found


def looserLimits(validators: list[SourceFile], written: set[str], schema: dict, subjects: Subjects) -> list[dict]:
    ordered = sorted(validators, key=lambda validator: validator.relative)
    return [finding for validator in ordered
            for finding in looserLimitsIn(validator, subjects.tableByLine(validator), written, schema)]


def readDoor(door: SourceFile, reach: Reach, subjects: Subjects, patterns: dict, schema: dict) -> dict | None:
    reached = reach.closure(door)
    tables = set().union(*(tablesWritten(sourceFile, patterns["write"], schema) for sourceFile in reached))
    if not tables:
        return None
    validators = [sourceFile for sourceFile in reached if isValidator(sourceFile, patterns["validator"])]
    return {"file": door.relative, "tables": sorted(tables), "isGuarded": bool(validators),
            "looserLimits": looserLimits(validators, tables, schema, subjects)}


def readDoors(sourceFiles: list[SourceFile], inputRules: dict, schema: dict) -> list[dict]:
    patterns = {"write": [re.compile(pattern, re.I) for pattern in inputRules.get("writePatterns", [])],
                "validator": [re.compile(pattern) for pattern in inputRules.get("validatorPatterns", [])]}
    entryGlobs, exemptGlobs = inputRules.get("entryPointGlobs", []), inputRules.get("exemptGlobs", [])
    reach, subjects = Reach(sourceFiles), Subjects(sourceFiles, schema)
    doors = [sourceFile for sourceFile in sourceFiles if matchesAny(sourceFile.relative, entryGlobs) and not matchesAny(sourceFile.relative, exemptGlobs)]
    readings = [readDoor(door, reach, subjects, patterns, schema) for door in doors]
    return [reading for reading in readings if reading]
