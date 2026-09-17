"""Measures how deep a line walks into an object's insides: property hops in one unbroken chain.

order.customer.address.postcode is three hops. A method call is not a hop and ends the run, so a fluent
pipeline — db.Invoices.Where(...).Select(...).ToList() — is one hop deep however long it is.
"""
from __future__ import annotations

import re

HOP = re.compile(r"(?<=[\w\)\]>])\s*(?:\?\.|!\.|\.)\s*([A-Za-z_]\w*)(\s*(?:<[^<>()]*>)?\s*\()?")
FREE_PREFIXES_WHEN_UNSET = ["this.", "base.", "self.", "import.meta."]
MAX_DEPTH_WHEN_UNSET = 2


def withoutFreePrefixes(line: str, freePrefixes: list[str]) -> str:
    for prefix in freePrefixes:
        line = re.sub(rf"(?<![\w.]){re.escape(prefix)}", "", line)
    return line


def deepestChain(line: str, freePrefixes: list[str]) -> int:
    deepest = 0
    run = 0
    previousPropertyEnd = None
    for hop in HOP.finditer(withoutFreePrefixes(line, freePrefixes)):
        isCall = hop.group(2) is not None
        continuesTheRun = hop.start() == previousPropertyEnd
        run = 0 if isCall else (run + 1 if continuesTheRun else 1)
        previousPropertyEnd = None if isCall else hop.end()
        deepest = max(deepest, run)
    return deepest


def isTooDeep(line: str, proseRules: dict) -> bool:
    freePrefixes = proseRules.get("freeChainPrefixes", FREE_PREFIXES_WHEN_UNSET)
    return deepestChain(line, freePrefixes) > proseRules.get("maxMemberChainDepth", MAX_DEPTH_WHEN_UNSET)
