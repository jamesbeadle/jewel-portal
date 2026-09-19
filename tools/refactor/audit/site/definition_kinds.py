"""What a definition is made of: the kinds of node the widget notation names, and the node itself."""
from __future__ import annotations

from dataclasses import dataclass, field

WIDGET, PART, FINDING, ROW, COLUMN, REPEAT, MAYBE, EITHER, LEAF = (
    "widget", "part", "finding", "row", "column", "repeat", "maybe", "either", "leaf",
)


@dataclass
class Definition:
    kind: str
    name: str = ""
    condition: str = ""
    count: str = ""
    line: int = 0
    children: list["Definition"] = field(default_factory=list)

    def key(self) -> tuple:
        return (self.kind, self.name, self.condition, self.count, tuple(child.key() for child in self.children))


def flatten(definitions: list[Definition]) -> list[Definition]:
    flat = []
    for definition in definitions:
        flat.append(definition)
        flat += flatten(definition.children)
    return flat
