"""A using the api gives every file for free, and the worker does not.

`api/GlobalUsings.cs` global-uses `Jewel.JPMS.Api.Data` and a few more; `worker/GlobalUsings.cs`
mirrors only some of them. A linked file that names `JpmsContext` with no `using` of its own
compiles in the api and fails in the worker, and nothing in the source says so — the second
shape of the same break, found on 23 September 2026 when the H&S digest sweep reached the
worker build. This reads both files and asks, per linked file, whether a type from an api-only
global namespace is named without the using that the worker needs.
"""

import re

from . import csharp

GLOBAL_USING = re.compile(r"^\s*global\s+using\s+([\w.]+)\s*;", re.MULTILINE)
USING = re.compile(r"^\s*using\s+([\w.]+)\s*;", re.MULTILINE)
NAMESPACE = re.compile(r"^\s*namespace\s+([\w.]+)", re.MULTILINE)


def _global_usings(path):
    return set(GLOBAL_USING.findall(csharp.read(path))) if path.is_file() else set()


def api_only_global_namespaces(repository):
    api = _global_usings(repository / "api" / "GlobalUsings.cs")
    worker = _global_usings(repository / "worker" / "GlobalUsings.cs")
    return api - worker


def types_declared_in(repository, namespaces):
    """Type name → namespace, for every repository type declared in one of the namespaces."""
    homes = {}
    for path in (repository / "api").rglob("*.cs"):
        source = csharp.read(path)
        declared = NAMESPACE.search(source)
        if declared is None or declared.group(1) not in namespaces:
            continue
        for name in csharp.DECLARATION.findall(source):
            homes[name] = declared.group(1)
    return homes


def _is_within(own_namespace, namespace):
    return own_namespace == namespace or own_namespace.startswith(namespace + ".")


def missing_usings(path, homes):
    source = csharp.read(path)
    code = csharp.code_only(source)
    own = NAMESPACE.search(source)
    own_namespace = own.group(1) if own else ""
    usings = set(USING.findall(source))
    reached = csharp.names_reached_for(code)
    missing = {}
    for name in sorted(reached & homes.keys()):
        namespace = homes[name]
        is_reachable = namespace in usings or _is_within(own_namespace, namespace)
        if not is_reachable:
            missing[name] = namespace
    return missing
