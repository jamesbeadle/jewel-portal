"""Which table a validator speaks for: each declaration in it names a subject, and the subject names a table.

A validator declares its subject — `class ProjectEntity`, `class CreateProjectValidator : AbstractValidator<CreateProjectCommand>`,
`const projectSchema = z.object({` — and the limits beneath a declaration are that subject's, so they are compared with the
columns of the subject's table and no other. The table is the one the repository maps the subject to (`DbSet<ProjectEntity>
Projects`, `Entity<ProjectEntity>().ToTable("Projects")`, `[Table("Projects")]`), else the one the subject's own name ends with
once its role suffix (Entity, Validator, Dto, Request, Command, Schema) is dropped.
"""
from __future__ import annotations

import re

from ..declarations import TYPE_DECLARATION
from ..source_files import SourceFile
from ..words import pluralsOf, wordsOf
from .schema_columns import tableKey

VALIDATED_TYPE = re.compile(r"(?:AbstractValidator|IValidator|IEntityTypeConfiguration)<(\w+)>")
SCHEMA_DECLARATION = re.compile(r"^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\b")
TABLE_ATTRIBUTE = re.compile(r"\[Table\(\s*\"(?P<table>\w+)\"")
SET_MAPPING = re.compile(r"DbSet<(?P<subject>\w+)>\s+(?P<table>\w+)")
TABLE_MAPPING = re.compile(r"Entity<(?P<subject>\w+)>\(\)\s*\.ToTable\(\s*\"(?P<table>\w+)\"")
ROLE_SUFFIXES = {"entity", "validator", "dto", "model", "request", "command", "query", "input", "in", "out", "schema",
                 "form", "payload", "create", "update"}


def declaredSubject(line: str) -> str | None:
    declared = VALIDATED_TYPE.search(line) or TYPE_DECLARATION.match(line) or SCHEMA_DECLARATION.match(line)
    return declared.group(1) if declared else None


def subjectKeys(subject: str) -> list[str]:
    words = wordsOf(subject)
    stems = ["".join(words)]
    while len(words) > 1 and words[-1] in ROLE_SUFFIXES:
        words = words[:-1]
        stems.append("".join(words))
    return [tableKey(form) for stem in stems for form in sorted(pluralsOf(stem))]


def mappedTables(sourceFiles: list[SourceFile]) -> dict[str, str]:
    mapped = {}
    for pattern in (SET_MAPPING, TABLE_MAPPING):
        for sourceFile in sourceFiles:
            for line in sourceFile.lines:
                for found in pattern.finditer(line):
                    mapped[found.group("subject")] = tableKey(found.group("table"))
    return mapped


class Subjects:
    def __init__(self, sourceFiles: list[SourceFile], schema: dict):
        self.schema = schema
        self.mapped = mappedTables(sourceFiles)
        self.tablesByFile: dict[str, list[str | None]] = {}

    def tableOf(self, subject: str) -> str | None:
        mapped = self.mapped.get(subject)
        if mapped in self.schema:
            return mapped
        keys = subjectKeys(subject)
        exact = [key for key in keys if key in self.schema]
        if exact:
            return exact[0]
        suffixed = sorted((table for table in self.schema if any(key.endswith(table) for key in keys)), key=len)
        return suffixed[-1] if suffixed else None

    def tableByLine(self, validator: SourceFile) -> list[str | None]:
        if validator.relative not in self.tablesByFile:
            self.tablesByFile[validator.relative] = self.readTableByLine(validator)
        return self.tablesByFile[validator.relative]

    def readTableByLine(self, validator: SourceFile) -> list[str | None]:
        tables, current, attributed = [], None, None
        for line in validator.lines:
            attribute = TABLE_ATTRIBUTE.search(line)
            if attribute:
                attributed = tableKey(attribute.group("table"))
            subject = declaredSubject(line)
            if subject:
                current = attributed if attributed in self.schema else self.tableOf(subject)
                attributed = None
            tables.append(current)
        return tables
