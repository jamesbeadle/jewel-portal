"""An api file the worker compiles may only reach for a type the worker compiles — with a using the worker has.

`worker/Jewel.JPMS.Worker.csproj` links a named subset of `api/` by Compile Include, so a linked
file is compiled twice: once with the whole api, once with the worker's much smaller set. A type
it reaches for in its own namespace needs no using, so nothing in the source says the worker
cannot see it — the api build stays green and the worker build fails. That cost three red mains
on 21 September 2026. The second shape (23 September): a type the worker DOES compile, named
through a global using only the api has — `JpmsContext` with no `using Jewel.JPMS.Api.Data;`.
Neither project can be compiled from a cloud session, so this stands in for the build.

    python3 -m tools.worker_link_check.check .
"""

import sys
from pathlib import Path

from . import csharp
from . import globals as global_usings
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


def usings_findings(repository, linked):
    homes = global_usings.types_declared_in(repository, global_usings.api_only_global_namespaces(repository))
    for path in linked:
        missing = global_usings.missing_usings(path, homes)
        if missing:
            yield path.relative_to(repository), missing


def report(repository):
    linked = files_the_worker_compiles(repository)
    broken = list(findings(repository, linked))
    unreachable_by_using = list(usings_findings(repository, linked))
    for path, named in broken:
        print(f"FAIL  {path} reaches for {', '.join(named)}, which the worker does not compile")
    for path, missing in unreachable_by_using:
        needed = ", ".join(f"{name} (using {namespace};)" for name, namespace in missing.items())
        print(f"FAIL  {path} names {needed} through a global using only the api has")
    if broken:
        print(f"\n{len(broken)} file(s) will not compile in the worker. Either add the type's file to")
        print("worker/Jewel.JPMS.Worker.csproj, or move the type to contracts/ if all three projects want it.")
    if unreachable_by_using:
        print(f"\n{len(unreachable_by_using)} file(s) will not compile in the worker: add the using named above to the file.")
    if broken or unreachable_by_using:
        return 1
    print(f"Worker link check passed: {len(linked)} linked api files reach for nothing the worker lacks.")
    return 0


def main():
    repository = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    return report(repository)


if __name__ == "__main__":
    raise SystemExit(main())
