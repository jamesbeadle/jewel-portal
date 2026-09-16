"""Reads a shell command as the git and gh invocations it makes, in order."""
from __future__ import annotations

import os
import re
import shlex

SEGMENT_SEPARATOR = re.compile(r"\s*(?:&&|\|\||;|\||\n)\s*")
WRAPPER_PROGRAMS = {"sudo", "command", "env", "nice", "time"}
ENVIRONMENT_ASSIGNMENT = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*=")
GUARDED_PROGRAMS = {"git", "gh"}
GLOBAL_OPTIONS_WITH_VALUE = {"-C", "-c", "--git-dir", "--work-tree", "--namespace"}
SWITCHING_SUBCOMMANDS = {"checkout", "switch"}
CREATING_OPTIONS = {"-b", "-B", "-c", "-C", "--orphan"}

Invocation = tuple[str, str, list[str]]


def invocationsIn(command: str) -> list[Invocation]:
    invocations = []
    for segment in SEGMENT_SEPARATOR.split(command):
        invocation = parseInvocation(segment)
        if invocation is not None:
            invocations.append(invocation)
    return invocations


def parseInvocation(segment: str) -> Invocation | None:
    tokens = tokensOf(segment)
    while tokens and (tokens[0] in WRAPPER_PROGRAMS or ENVIRONMENT_ASSIGNMENT.match(tokens[0])):
        tokens.pop(0)
    if not tokens or tokens[0] not in GUARDED_PROGRAMS:
        return None
    program, rest = tokens[0], tokens[1:]
    while rest and rest[0].startswith("-"):
        option = rest.pop(0)
        if option in GLOBAL_OPTIONS_WITH_VALUE and rest:
            rest.pop(0)
    if not rest:
        return None
    return program, rest[0], rest[1:]


def tokensOf(segment: str) -> list[str]:
    try:
        return shlex.split(segment)
    except ValueError:
        return segment.split()


def branchAfterSwitch(subcommand: str, arguments: list[str], branch: str | None) -> str | None:
    if subcommand not in SWITCHING_SUBCOMMANDS:
        return branch
    positional = [argument for argument in arguments if not argument.startswith("-")]
    if not positional:
        return branch
    isCreating = any(argument in CREATING_OPTIONS for argument in arguments)
    if isCreating:
        return positional[0]
    if "--" in arguments or looksLikePath(positional[0]):
        return branch
    return positional[0]


def looksLikePath(argument: str) -> bool:
    return "/" in argument and (os.path.exists(argument) or argument.startswith("."))
