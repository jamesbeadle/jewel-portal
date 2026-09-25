"""Reads the tables the schema declares and the longest text each column accepts: SQL and EF Core migrations."""
from __future__ import annotations

import re
from pathlib import Path

from ..source_files import matchesAny
from ..words import wordsOf

CREATE_TABLE = re.compile(r"create\s+table\s+(?:if\s+not\s+exists\s+)?(?P<name>[\w.\"`\[\]]+)\s*\((?P<body>.*?)\)\s*;", re.I | re.S)
ALTER_ADD = re.compile(r"alter\s+table\s+(?:only\s+)?(?P<name>[\w.\"`\[\]]+)\s+add\s+(?:column\s+)?(?:if\s+not\s+exists\s+)?(?P<body>[^;]+);", re.I)
EF_TABLE = re.compile(r"CreateTable\(\s*name:\s*\"(?P<name>\w+)\"")
EF_COLUMN = re.compile(r"^\s*(?P<column>\w+)\s*=\s*table\.Column<", re.M)
EF_ADD_COLUMN = re.compile(r"AddColumn<[^>]+>\(\s*name:\s*\"(?P<column>\w+)\",\s*table:\s*\"(?P<name>\w+)\"(?P<rest>.*?)\);", re.S)
TEXT_LIMIT = re.compile(r"(?:char(?:acter)?(?:\s+varying)?|varchar|nvarchar|nchar)\s*\(\s*(\d+)\s*\)|maxLength:\s*(\d+)", re.I)
CONSTRAINT_WORDS = {"constraint", "primary", "foreign", "unique", "check", "key", "index", "exclude"}
EXCLUDED_WHEN_UNSET = ["**/node_modules/**", "**/bin/**", "**/obj/**", "**/.svelte-kit/**"]


def tableKey(name: str) -> str:
    key = "".join(wordsOf(name.split(".")[-1]))
    if key.endswith("ies"):
        return key[:-3] + "y"
    return key[:-1] if key.endswith("s") else key


def columnKey(name: str) -> str:
    return "".join(wordsOf(name))


def textLimit(definition: str) -> int | None:
    found = TEXT_LIMIT.search(definition)
    return int(found.group(1) or found.group(2)) if found else None


def topLevelItems(body: str) -> list[str]:
    items, depth, current = [], 0, ""
    for character in body:
        depth += {"(": 1, ")": -1}.get(character, 0)
        if character == "," and depth == 0:
            items.append(current)
            current = ""
            continue
        current += character
    return [*items, current]


def sqlColumns(body: str) -> dict[str, int | None]:
    columns = {}
    for item in topLevelItems(body):
        words = item.split()
        if not words or words[0].lower() in CONSTRAINT_WORDS:
            continue
        columns[columnKey(words[0].strip("\"`[]"))] = textLimit(item)
    return columns


def efTables(text: str) -> dict[str, dict]:
    tables = {}
    starts = list(EF_TABLE.finditer(text))
    for index, start in enumerate(starts):
        end = starts[index + 1].start() if index + 1 < len(starts) else len(text)
        section = text[start.end():end]
        lines = {found.group("column"): section[found.start():].split("\n", 1)[0] for found in EF_COLUMN.finditer(section)}
        tables[tableKey(start.group("name"))] = {columnKey(column): textLimit(line) for column, line in lines.items()}
    return tables


def tablesIn(text: str) -> dict[str, dict]:
    tables = efTables(text)
    for found in CREATE_TABLE.finditer(text):
        tables.setdefault(tableKey(found.group("name").strip("\"`[]")), {}).update(sqlColumns(found.group("body")))
    for found in ALTER_ADD.finditer(text):
        tables.setdefault(tableKey(found.group("name").strip("\"`[]")), {}).update(sqlColumns(found.group("body")))
    for found in EF_ADD_COLUMN.finditer(text):
        tables.setdefault(tableKey(found.group("name")), {})[columnKey(found.group("column"))] = textLimit(found.group("rest"))
    return tables


def readSchema(repositoryRoot: Path, inputRules: dict) -> dict[str, dict]:
    excluded = inputRules.get("schemaExcludeGlobs", EXCLUDED_WHEN_UNSET)
    tables: dict[str, dict] = {}
    for pattern in inputRules.get("schemaGlobs", []):
        for path in sorted(repositoryRoot.glob(pattern)):
            relative = path.relative_to(repositoryRoot).as_posix()
            if not path.is_file() or matchesAny(relative, excluded):
                continue
            for table, columns in tablesIn(path.read_text(encoding="utf-8", errors="replace")).items():
                tables.setdefault(table, {}).update(columns)
    return tables
