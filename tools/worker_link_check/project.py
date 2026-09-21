"""The api files the worker's project file links, with its Compile Include globs expanded."""

import re
from pathlib import Path

LINKED_API_FILE = re.compile(r'Include="(\.\.\\api\\[^"]+)"')

WORKER_PROJECT = Path("worker") / "Jewel.JPMS.Worker.csproj"


def _as_repository_path(repository, include):
    return (repository / "worker" / include.replace("\\", "/")).resolve()


def files_the_worker_compiles(repository):
    project = repository / WORKER_PROJECT
    includes = LINKED_API_FILE.findall(project.read_text(encoding="utf-8"))
    linked = set()
    for include in includes:
        candidate = _as_repository_path(repository, include)
        hasAGlob = "*" in include
        if hasAGlob:
            root, pattern = _split_on_the_glob(repository, include)
            linked.update(path for path in root.glob(pattern) if path.is_file())
            continue
        if candidate.is_file():
            linked.add(candidate)
    return sorted(linked)


def _split_on_the_glob(repository, include):
    parts = include.replace("\\", "/").split("/")
    firstGlob = next(index for index, part in enumerate(parts) if "*" in part)
    root = (repository / "worker" / "/".join(parts[:firstGlob])).resolve()
    return root, "/".join(parts[firstGlob:])
