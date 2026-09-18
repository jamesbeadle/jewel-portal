"""Every bare type name a C# file uses, resolved against the namespaces that file can see.

WHY THIS EXISTS. The api cannot be compiled in a cloud session — it pulls eighteen real NuGet
packages and the registry is unreachable from both the sandbox and the device (see the 17 Sept
offline-build finding). So a missing `using` is invisible until CI, which is how PR #54 shipped a
handler whose return type named an entity its file no longer imported.

WHAT IT DOES. It reads the repository's own declarations. A bare PascalCase name in a type
position that IS declared somewhere here, and is NOT reachable from the file's usings, its
namespace chain or the project's global usings, is a build error waiting to happen.

WHAT IT IS NOT. Not a compiler and not a gate. It cannot see NuGet or framework types, and it does
not check members, arity or ambiguity. It has two known false-positive shapes, both harmless once
you can name them:
  - a positional record's PARAMETER NAME that happens to match a type elsewhere, as in
    `record CarriedFiles(IReadOnlyList<MailboxDraftAttachment> Attachments, string CoverNote)`;
  - a CHILD NAMESPACE used as a qualifier where a type of the same name exists, as in
    `Compose.OutboundEmailDispatcher` inside the MailboxIntake namespace.
Generic page and component names — Draft, Status, Line, Section, Claim — are where both show up.
Read its output; do not gate on it.

    python3 -m tools.build.resolve_type_names                  # the whole repository
    python3 -m tools.build.resolve_type_names <file> [<file>]  # what a branch changed
"""
import collections
import pathlib
import re
import sys

PROJECTS = ("api", "contracts", "jpms", "worker", "tests")

DECLARATION = re.compile(
    r"^\s*(?:public|internal|private|protected|sealed|static|abstract|partial|\s)*"
    r"\b(?:class|record|struct|interface|enum)\s+([A-Z]\w*)")
NAMESPACE = re.compile(r"^\s*namespace\s+([\w.]+)")
USING = re.compile(r"^\s*(?:global\s+)?using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;")
STRING = re.compile(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', re.S)
BLOCK_COMMENT = re.compile(r"/\*.*?\*/", re.S)
LINE_COMMENT = re.compile(r"//.*")

# A name in a TYPE position: not preceded by a dot (member access, or a qualified name), not
# followed by a colon (a named argument), not assigned to (an object initialiser's property) and
# not called (a method, which may be declared in a sibling partial).
TYPE_NAME = re.compile(r"(?<![.\w])(?:(?P<constructed>new)\s+)?(?P<name>[A-Z]\w*)\b(?!\s*:(?!:))(?P<tail>\s*[=(])?")


def sourceFiles(root, area):
    for path in (root / area).rglob("*.cs"):
        if not {"obj", "bin"} & set(path.parts): yield path


def readHeader(text):
    namespace, usings, declared = "", set(), set()
    for line in text.splitlines():
        found = NAMESPACE.match(line)
        if found: namespace = found.group(1)
        if line.lstrip().startswith(("using ", "global using ")):
            found = USING.match(line)
            if found: usings.add(found.group(1))
        found = DECLARATION.match(line)
        if found: declared.add(found.group(1))
    return namespace, usings, declared


def survey(root):
    """Where every type in the repository is declared, and each project's global usings."""
    declarations, globalUsings = collections.defaultdict(set), collections.defaultdict(set)
    for area in PROJECTS:
        for path in sourceFiles(root, area):
            text = path.read_text(errors="ignore")
            namespace, _, declared = readHeader(text)
            for name in declared:
                declarations[name].add(namespace)
            for line in text.splitlines():
                if not line.lstrip().startswith("global using"): continue
                found = USING.match(line)
                if found: globalUsings[area].add(found.group(1))
    return declarations, globalUsings


def namesUsedAsTypes(text):
    body = LINE_COMMENT.sub("", BLOCK_COMMENT.sub("", STRING.sub('""', text)))
    body = "\n".join(
        line for line in body.splitlines()
        if not line.lstrip().startswith(("using ", "global using ", "namespace ")))
    for match in TYPE_NAME.finditer(body):
        tail = (match.group("tail") or "").strip()
        if tail == "=": continue
        if tail == "(" and not match.group("constructed"): continue
        yield match.group("name")


def unreachableIn(root, name, declarations, globalUsings):
    path = root / name
    if not path.exists(): return []
    text = path.read_text(errors="ignore")
    namespace, usings, declared = readHeader(text)
    parts = namespace.split(".")
    reachable = (usings
                 | {".".join(parts[:index]) for index in range(1, len(parts) + 1)}
                 | globalUsings.get(name.split("/")[0], set())
                 | {""})
    return [f"{name}: '{used}' is declared only in {sorted(declarations[used])}"
            for used in sorted(set(namesUsedAsTypes(text)))
            if used not in declared and used in declarations and not declarations[used] & reachable]


def main():
    root = pathlib.Path(".")
    declarations, globalUsings = survey(root)
    names = sys.argv[1:] or [str(path) for area in PROJECTS for path in sourceFiles(root, area)]
    problems = [line for name in names for line in unreachableIn(root, name, declarations, globalUsings)]
    print("\n".join(problems) if problems else f"{len(names)} files read; every type name resolves")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
