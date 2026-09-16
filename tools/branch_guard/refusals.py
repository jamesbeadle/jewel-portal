"""The rules: which invocations would move the default branch, and what to say instead."""
from __future__ import annotations

WRITES_HISTORY = {"commit", "merge", "rebase", "cherry-pick", "revert", "am"}
REFERENCE_PREFIX = "refs/heads/"


def refuseGit(subcommand: str, arguments: list[str], branch: str | None, defaultBranch: str) -> str | None:
    if subcommand in WRITES_HISTORY and branch == defaultBranch:
        return (
            f"Branch guard: `git {subcommand}` on `{defaultBranch}` is not allowed — the default branch only "
            f"moves by a reviewed pull request. Create a branch first: `git switch -c fix/<slug>` for a FIX "
            f"task or `git switch -c feature/<slug>` for a story, then commit there and open the pull request."
        )
    if subcommand == "push" and pushTargetsDefault(arguments, branch, defaultBranch):
        return (
            f"Branch guard: this push would move `{defaultBranch}`, which only moves by a reviewed pull "
            f"request. Push the fix/ or feature/ branch instead (`git push -u origin <branch>`) and open the "
            f"pull request for the person to merge."
        )
    return None


def pushTargetsDefault(arguments: list[str], branch: str | None, defaultBranch: str) -> bool:
    references = [argument for argument in arguments if not argument.startswith("-")][1:]
    if not references:
        return branch == defaultBranch
    return any(referenceTargets(reference, branch, defaultBranch) for reference in references)


def referenceTargets(reference: str, branch: str | None, defaultBranch: str) -> bool:
    destination = reference.split(":")[-1] if ":" in reference else reference
    destination = destination.removeprefix("+").removeprefix(REFERENCE_PREFIX)
    if destination == defaultBranch:
        return True
    return reference == "HEAD" and branch == defaultBranch


def refuseGithubMerge(subcommand: str, arguments: list[str]) -> str | None:
    if subcommand == "pr" and arguments[:1] == ["merge"]:
        return (
            "Branch guard: `gh pr merge` is not allowed — a person reviews and merges the pull request. "
            "Push the branch, make sure the pull request is open, and say on the task that it is ready."
        )
    return None
