"""What git says about the repository the command runs in."""
from __future__ import annotations

import subprocess


def defaultBranchOf(repository: str) -> str | None:
    if git(repository, "rev-parse", "--is-inside-work-tree") != "true":
        return None
    origin = git(repository, "symbolic-ref", "--short", "refs/remotes/origin/HEAD")
    if origin:
        return origin.removeprefix("origin/")
    for candidate in ("main", "master"):
        if git(repository, "rev-parse", "--verify", "--quiet", f"refs/heads/{candidate}"):
            return candidate
    return "main"


def currentBranchOf(repository: str) -> str | None:
    return git(repository, "symbolic-ref", "--short", "HEAD") or None


def git(repository: str, *arguments: str) -> str:
    try:
        completed = subprocess.run(
            ["git", "-C", repository, *arguments], capture_output=True, text=True, check=False
        )
    except OSError:
        return ""
    return completed.stdout.strip() if completed.returncode == 0 else ""
