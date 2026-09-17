"""Recognises the line a function starts on, for C-family and TypeScript sources."""
from __future__ import annotations

import re

C_FAMILY_SIGNATURE = re.compile(
    r"^\s*(?:public|private|protected|internal|static|async|override|sealed|partial|virtual)"
    r"[\w\s<>,\[\]\?]*\s+\w+\s*\([^;]*$|^\s*(?:public|private|protected|internal)"
    r"[\w\s<>,\[\]\?]*\s+\w+\s*\([^)]*\)\s*$"
)
TYPESCRIPT_SIGNATURE = re.compile(
    r"^\s*(?:export\s+)?(?:default\s+)?(?:async\s+)?function\s*\*?\s*\w*\s*(?:<[^>]*>)?\s*\("
    r"|^\s*(?:export\s+)?(?:const|let)\s+\w+\s*(?::[^=]+)?=\s*(?:async\s*)?(?:\([^)]*\)|\w+)\s*(?::\s*[^=]+)?=>\s*\{\s*$"
)


def isFunctionSignature(line: str) -> bool:
    return bool(C_FAMILY_SIGNATURE.match(line) or TYPESCRIPT_SIGNATURE.match(line))
