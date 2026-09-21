"""An api file the worker compiles may only reach for a type the worker compiles.

`worker/Jewel.JPMS.Worker.csproj` links a named subset of `api/` by Compile Include, so a linked
file is compiled twice: once with the whole api, once with the worker's much smaller set. A type
it reaches for in its own namespace needs no using, so nothing in the source says the worker
cannot see it — the api build stays green and the worker build fails. That cost three red mains
on 21 September 2026. Neither project can be compiled from a cloud session, so this stands in for
the build.

    python3 -m tools.worker_link_check.check .
"""

import sys
from pathlib import Path

from . import csharp
from .project import files_the_worker_compiles


def api_files(repository):
    return sorted((repository / "api").rglob("*.cs"))


def types_the_worker_compiles(repository, linked):
    own = sorted((repository / "worker").rglob("*.cs"))
    shared = sorted((repository / "contracts").rglob("*.cs"))
    return csharp.declared_types(linked + own + shared)


def unreachable_types(repository, linked):
    return csharp.declared_types(api_files(repository)) - types_the_worker_compiles(repository, linked)


def findings(repository, linked):
    unreachable = unreachable_types(repository, linked)
    for path in linked:
        source = csharp.code_only(csharp.read(path))
        named = sorted(csharp.names_reached_for(source) & unreachable)
        if named:
            yield path.relative_to(repository), named


def report(repository):
    linked = files_the_worker_compiles(repository)
    broken = list(findings(repository, linked))
    for path, named in broken:
        print(f"FAIL  {path} reaches for {', '.join(named)}, which the worker does not compile")
    if broken:
        print(f"\n{len(broken)} file(s) will not compile in the worker. Either add the type's file to")
        print("worker/Jewel.JPMS.Worker.csproj, or move the type to contracts/ if all three projects want it.")
        return 1
    print(f"Worker link check passed: {len(linked)} linked api files reach for nothing the worker lacks.")
    return 0


def main():
    repository = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    return report(repository)


if __name__ == "__main__":
    raise SystemExit(main())
