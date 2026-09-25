"""Finds the doors that write to the database without checking what they write against the columns.

Every entry point that writes — an API route, a server action, a command handler, an MCP tool or action
handler — has to check its input itself, because anything can call it without passing through a form. The
schema says what each column accepts; a door with no validator anywhere it reaches is unguarded, and a
validator that allows more than its column holds has drifted from the schema. Not measured until the
repository states a schema and has a door that writes to it.
"""
from __future__ import annotations

from pathlib import Path

from ..inputs.doors import readDoors
from ..inputs.schema_columns import readSchema
from ..source_files import SourceFile

OFFENDER_LIMIT = 100


def check(repositoryRoot: Path, sourceFiles: list[SourceFile], rules: dict) -> dict:
    inputRules = rules.get("inputValidation", {})
    schema = readSchema(repositoryRoot, inputRules)
    doors = readDoors(sourceFiles, inputRules, schema) if schema else []
    unguarded = [{"file": door["file"], "tables": door["tables"]} for door in doors if not door["isGuarded"]]
    looser = [limit for door in doors for limit in door["looserLimits"]]
    summary = {
        "schemaTables": len(schema),
        "limitedColumns": sum(1 for columns in schema.values() for limit in columns.values() if limit),
        "writeDoors": len(doors),
        "unvalidatedDoors": len(unguarded),
        "looserLimits": len(looser),
    }
    return {"name": "inputValidation", "summary": summary,
            "offenders": {"unvalidated": unguarded[:OFFENDER_LIMIT], "looserLimits": looser[:OFFENDER_LIMIT]}}
