"""The branch guard: nothing lands on the default branch from a Claude session.

Installed by the project-process kit as a Claude Code PreToolUse hook on the Bash tool
(.claude/settings.json), run as `python3 tools/branch_guard`. Claude Code hands the tool call
to stdin as JSON; the guard reads the command, follows any checkout or switch it makes, and
refuses — exit 2, the reason on stderr — any commit, merge, rebase, cherry-pick or revert that
would write to the default branch, any push that would move it, and any pull-request merge.
Everything else passes, and a guard that breaks lets the command through rather than the shell down.
"""
from __future__ import annotations

import json
import os
import re
import sys

from command_parser import invocationsIn, branchAfterSwitch
from refusals import refuseGit, refuseGithubMerge
from repository import currentBranchOf, defaultBranchOf

BLOCKED_EXIT = 2
MENTIONS_GIT = re.compile(r"\b(git|gh)\b")


def main() -> int:
    payload = readPayload()
    if payload is None or payload.get("tool_name") != "Bash":
        return 0
    command = str(payload.get("tool_input", {}).get("command", ""))
    if not MENTIONS_GIT.search(command):
        return 0
    repository = payload.get("cwd") or os.getcwd()
    defaultBranch = defaultBranchOf(repository)
    if defaultBranch is None:
        return 0
    refusal = firstRefusal(command, currentBranchOf(repository), defaultBranch)
    if refusal is None:
        return 0
    sys.stderr.write(refusal + "\n")
    return BLOCKED_EXIT


def readPayload() -> dict | None:
    try:
        return json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        return None


def firstRefusal(command: str, branch: str | None, defaultBranch: str) -> str | None:
    for program, subcommand, arguments in invocationsIn(command):
        if program == "gh":
            refusal = refuseGithubMerge(subcommand, arguments)
        else:
            branch = branchAfterSwitch(subcommand, arguments, branch)
            refusal = refuseGit(subcommand, arguments, branch, defaultBranch)
        if refusal:
            return refusal
    return None


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        sys.exit(0)
