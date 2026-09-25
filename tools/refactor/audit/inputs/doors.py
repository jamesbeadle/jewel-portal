"""The doors into the data: each entry point, the tables it writes through everything it reaches, and its checks.

A door writes a table when it, or a file it reaches, matches a write pattern naming a table the schema declares.
It is guarded when it, or a file it reaches, matches a validator pattern. A validator that names a column and
allows it more characters than the column holds is a looser limit: the check and the schema have drifted.
"""
from __future__ import annotations

import re

from ..source_files import SourceFile, matchesAny
from .reach import Reach
from .schema_columns import columnKey, tableKey

LIMIT_IN_VALIDATOR = re.compile(r"(?:\bmax|maxLength|MaxLength|MaximumLength|StringLength|Length)\s*(?:\(\s*|[:=]\s*)(?:\d+\s*,\s*)?(\d+)")
NAME = re.compile(r"[A-Za-z_]\w*")


def tablesWritten(sourceFile: SourceFile, writePatterns: list, schema: dict) -> set[str]:
    text = "\n".join(sourceFile.lines)
    named = {tableKey(found.group("table")) for pattern in writePatterns for found in pattern.finditer(text)}
    return {table for table in named if table in schema}


def isValidator(sourceFile: SourceFile, validatorPatterns: list) -> bool:
    return any(pattern.search(line) for line in sourceFile.lines for pattern in validatorPatterns)


def looserLimits(validators: list[SourceFile], tables: set[str], schema: dict) -> list[dict]:
    limits = {column: limit for table in tables for column, limit in schema[table].items() if limit}
    found = []
    for validator in validators:
        for number, line in enumerate(validator.lines, start=1):
            allowed = LIMIT_IN_VALIDATOR.search(line)
            columns = {columnKey(name) for name in NAME.findall(line)} & set(limits)
            found += [{"file": validator.relative, "line": number, "column": column, "columnLimit": limits[column],
                       "validatorLimit": int(allowed.group(1))}
                      for column in columns if allowed and int(allowed.group(1)) > limits[column]]
    return found


def readDoor(door: SourceFile, reach: Reach, patterns: dict, schema: dict) -> dict | None:
    reached = reach.closure(door)
    tables = set().union(*(tablesWritten(sourceFile, patterns["write"], schema) for sourceFile in reached))
    if not tables:
        return None
    validators = [sourceFile for sourceFile in reached if isValidator(sourceFile, patterns["validator"])]
    return {"file": door.relative, "tables": sorted(tables), "isGuarded": bool(validators),
            "looserLimits": looserLimits(validators, tables, schema)}


def readDoors(sourceFiles: list[SourceFile], inputRules: dict, schema: dict) -> list[dict]:
    patterns = {"write": [re.compile(pattern, re.I) for pattern in inputRules.get("writePatterns", [])],
                "validator": [re.compile(pattern) for pattern in inputRules.get("validatorPatterns", [])]}
    entryGlobs, exemptGlobs = inputRules.get("entryPointGlobs", []), inputRules.get("exemptGlobs", [])
    reach = Reach(sourceFiles)
    doors = [sourceFile for sourceFile in sourceFiles if matchesAny(sourceFile.relative, entryGlobs) and not matchesAny(sourceFile.relative, exemptGlobs)]
    readings = [readDoor(door, reach, patterns, schema) for door in doors]
    return [reading for reading in readings if reading]
