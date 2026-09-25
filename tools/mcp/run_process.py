"""Runs one named process: its mechanical steps here, its doctrine back to the caller for the judgement.

Which part of a process a script can do and which part needs reading and deciding is the kit's business, not
the caller's, so one answer carries both: what ran, what it printed, and the doctrine to follow from there.
"""
from __future__ import annotations

import subprocess
from pathlib import Path

SKILL_DIRECTORY = Path(".claude") / "skills"
STEP_TIMEOUT_SECONDS = 900


def runCommand(command: list, repositoryRoot: Path) -> str:
    finished = subprocess.run(command, cwd=repositoryRoot, capture_output=True, text=True,
                              timeout=STEP_TIMEOUT_SECONDS, check=False)
    printed = (finished.stdout + finished.stderr).strip()
    return f"$ {' '.join(command)}\n(exit {finished.returncode})\n{printed}"


def doctrineFor(process: dict, repositoryRoot: Path) -> str:
    skill = process["skill"]
    if not skill:
        return ""
    doctrinePath = repositoryRoot / SKILL_DIRECTORY / skill / "SKILL.md"
    if not doctrinePath.exists():
        return f"The {skill} skill is not installed here; re-run the project-process bootstrap."
    return doctrinePath.read_text(encoding="utf-8")


def resultFor(process: dict, repositoryRoot: Path) -> str:
    parts = [f"# {process['title']}", process["summary"]]
    ran = [runCommand(command, repositoryRoot) for command in process["commands"]]
    if ran:
        parts.append("## What ran\n\n" + "\n\n".join(ran))
    doctrine = doctrineFor(process, repositoryRoot)
    if doctrine:
        parts.append("## Carry this out, and nothing else\n\n" + doctrine)
    return "\n\n".join(parts)
