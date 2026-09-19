"""The words the site is defined in: the catalogue of shared widgets, the site's own components, and the markup a widget should own."""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from pathlib import Path

from ..source_files import SourceFile
from .markup_tree import Node

CATALOGUE, COMPONENT, HTML, UNRESOLVED, IGNORED = "catalogue", "component", "html", "unresolved", "ignored"
GRID_COLUMNS = re.compile(r"^(?:\w+:)?grid-cols-(\d+)$")
BREAKPOINT_PREFIX = re.compile(r"^\w+:")
ONE_COLUMN = 1


@dataclass(frozen=True)
class HandRolledRule:
    element: str
    ownedBy: str
    unlessClassPrefixes: tuple[str, ...] = ()
    unlessAttributes: tuple[str, ...] = ()

    def applies(self, node: Node) -> bool:
        hasExemptClass = any(name.startswith(prefix) for name in node.classes for prefix in self.unlessClassPrefixes)
        hasExemptAttribute = any(text in node.attributes for text in self.unlessAttributes)
        return not hasExemptClass and not hasExemptAttribute


@dataclass
class Vocabulary:
    catalogue: set[str]
    componentFiles: dict[str, SourceFile]
    handRolled: dict[str, HandRolledRule]
    ignoredTags: set[str] = field(default_factory=set)
    horizontalClasses: list[str] = field(default_factory=lambda: ["flex", "inline-flex"])
    verticalClasses: list[str] = field(default_factory=lambda: ["flex-col"])

    def kindOf(self, tagName: str) -> str:
        if tagName in self.ignoredTags:
            return IGNORED
        if tagName in self.catalogue:
            return CATALOGUE
        if tagName in self.componentFiles:
            return COMPONENT
        return UNRESOLVED if tagName[:1].isupper() else HTML

    def owningWidget(self, node: Node) -> str | None:
        rule = self.handRolled.get(node.name.lower())
        if rule is None or not rule.applies(node):
            return None
        return rule.ownedBy

    def everyOwner(self) -> tuple[str, ...]:
        return tuple(rule.ownedBy for rule in self.handRolled.values())

    def isHorizontal(self, classes: list[str]) -> bool:
        bare = [BREAKPOINT_PREFIX.sub("", name) for name in classes]
        if any(name in self.verticalClasses for name in bare):
            return False
        if any(name in self.horizontalClasses for name in bare):
            return True
        return any(columnsOf(name) > ONE_COLUMN for name in classes)


def columnsOf(className: str) -> int:
    match = GRID_COLUMNS.match(className)
    return int(match.group(1)) if match else ONE_COLUMN


def handRolledRule(row: dict) -> HandRolledRule:
    return HandRolledRule(
        row["element"].lower(), row["ownedBy"], tuple(row.get("unlessClassPrefixes", [])), tuple(row.get("unlessAttributes", [])),
    )


def componentFilesOf(views: list[SourceFile], catalogue: set[str]) -> dict[str, SourceFile]:
    files = {}
    for view in views:
        stem = Path(view.relative).stem
        if stem not in catalogue and stem not in files:
            files[stem] = view
    return files


def vocabularyFor(settings: dict, views: list[SourceFile]) -> Vocabulary:
    catalogue = set(settings.get("catalogue", []))
    rules = {row["element"].lower(): handRolledRule(row) for row in settings.get("handRolled", [])}
    vocabulary = Vocabulary(catalogue, componentFilesOf(views, catalogue), rules, set(settings.get("ignoredTags", [])))
    if "horizontalClasses" in settings:
        vocabulary.horizontalClasses = settings["horizontalClasses"]
    if "verticalClasses" in settings:
        vocabulary.verticalClasses = settings["verticalClasses"]
    return vocabulary
