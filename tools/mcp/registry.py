"""The processes this kit offers, as the tools that run them: one tool per process, named for the process.

processes.json is the registry: a process is offered here only if it is listed there, and a listed process
names the skill that carries its doctrine, so a tool cannot exist without one.
"""
from __future__ import annotations

import json
from pathlib import Path

REGISTRY_PATH = Path("tools") / "mcp" / "processes.json"
NO_ARGUMENTS = {"type": "object", "properties": {}, "additionalProperties": False}


def loadProcesses(repositoryRoot: Path) -> list:
    registry = json.loads((repositoryRoot / REGISTRY_PATH).read_text(encoding="utf-8"))
    return registry["processes"]


def describe(process: dict) -> str:
    sentences = " / ".join(f'"{sentence}"' for sentence in process["sentences"])
    written = ", ".join(process["writes"]) or "nothing"
    branch = process["branch"] or "no branch - read-only, commits nothing, opens no pull request"
    return f"{process['summary']} Asked for as {sentences}. Writes: {written}. Runs on: {branch}."


def toolFor(process: dict) -> dict:
    return {"name": process["tool"], "description": describe(process), "inputSchema": NO_ARGUMENTS}


def toolsFrom(processes: list) -> list:
    return [toolFor(process) for process in processes]


def processNamed(processes: list, toolName: str) -> dict | None:
    for process in processes:
        if process["tool"] == toolName:
            return process
    return None
