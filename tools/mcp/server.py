"""The project-process server: every process the kit offers, as a tool named for that process.

Naming a tool runs that process and no other - there is no nearest match to fall into, which is the point.
It speaks the protocol over stdin and stdout in the standard library alone, so a repository installs nothing.

Usage: python3 tools/mcp/server.py [repositoryRoot]
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

from registry import loadProcesses, processNamed, toolsFrom
from run_process import resultFor

PROTOCOL_VERSION = "2024-11-05"
VERSION_PATH = Path("tools") / "refactor" / "kit-version"
METHOD_NOT_FOUND = -32601


def serverDetails(repositoryRoot: Path) -> dict:
    versionPath = repositoryRoot / VERSION_PATH
    version = versionPath.read_text(encoding="utf-8").strip() if versionPath.exists() else "unknown"
    return {"name": "project-process", "version": version}


def send(payload: dict) -> None:
    sys.stdout.write(json.dumps(payload) + "\n")
    sys.stdout.flush()


def respond(identifier: object, result: dict) -> None:
    send({"jsonrpc": "2.0", "id": identifier, "result": result})


def refuse(identifier: object, message: str) -> None:
    send({"jsonrpc": "2.0", "id": identifier, "error": {"code": METHOD_NOT_FOUND, "message": message}})


def asText(text: str) -> dict:
    return {"content": [{"type": "text", "text": text}]}


def namesOffered(processes: list) -> str:
    return ", ".join(process["tool"] for process in processes)


def answerCall(request: dict, repositoryRoot: Path) -> None:
    processes = loadProcesses(repositoryRoot)
    parameters = request.get("params", {})
    toolName = parameters.get("name", "")
    identifier = request.get("id")
    process = processNamed(processes, toolName)
    if process is None:
        refuse(identifier, f"The kit has no process called {toolName}. It offers: {namesOffered(processes)}.")
        return
    respond(identifier, asText(resultFor(process, repositoryRoot)))


def answer(request: dict, repositoryRoot: Path) -> None:
    method = request.get("method", "")
    identifier = request.get("id")
    if method == "initialize":
        respond(identifier, {"protocolVersion": PROTOCOL_VERSION, "capabilities": {"tools": {}},
                             "serverInfo": serverDetails(repositoryRoot)})
        return
    if method == "tools/list":
        respond(identifier, {"tools": toolsFrom(loadProcesses(repositoryRoot))})
        return
    if method == "tools/call":
        answerCall(request, repositoryRoot)
        return
    if identifier is not None:
        refuse(identifier, f"Unsupported method {method}.")


def main() -> int:
    repositoryRoot = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    for line in sys.stdin:
        if line.strip():
            answer(json.loads(line), repositoryRoot)
    return 0


if __name__ == "__main__":
    sys.exit(main())
