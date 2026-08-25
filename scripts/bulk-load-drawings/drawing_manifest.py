"""Step 1 of the bulk load: walk a folder of drawing files and write a manifest CSV.

One row per file. The directory path becomes the register folder path (sub-directories
become sub-folders); code / revision / title are guessed from the file name with the same
pattern Document Triage uses ("PRO-064-(WD)-P-800 Rev I Site set out.pdf") and are all
optional — fix or blank them in Excel before step 2.
"""
import csv
import re
from pathlib import Path

MANIFEST_COLUMNS = ["relative_path", "folder_path", "file_name", "code", "revision", "title"]
FOLDER_SEPARATOR = "/"

# code, then a separator, then "Rev X", then the title — DocumentControl.razor's pattern, with
# underscores accepted as separators too ("S-100_Rev_B.dwg").
FILE_NAME_PATTERN = re.compile(
    r"^(?P<code>.+?)[\s\-–—_]+Rev(?:ision)?\.?[\s_]*(?P<rev>[A-Za-z0-9]{1,3})\b[\s\-–—_]*(?P<title>.*)$",
    re.IGNORECASE)

SKIPPED_FILE_PREFIXES = (".", "~$")


def guess_fields(file_name: str) -> tuple[str, str, str]:
    """(code, revision, title) from a file name; the bare stem as the code when no Rev is found."""
    stem = Path(file_name).stem.strip()
    match = FILE_NAME_PATTERN.match(stem)
    if not match:
        return stem, "", ""
    return (match.group("code").strip(),
            match.group("rev").strip().upper(),
            match.group("title").strip())


def is_drawing_file(path: Path) -> bool:
    return path.is_file() and not path.name.startswith(SKIPPED_FILE_PREFIXES)


def build_rows(source: Path) -> list[dict]:
    rows = []
    for path in sorted(source.rglob("*")):
        if not is_drawing_file(path):
            continue
        relative = path.relative_to(source)
        folder_path = FOLDER_SEPARATOR.join(relative.parent.parts)
        code, revision, title = guess_fields(path.name)
        rows.append({
            "relative_path": relative.as_posix(),
            "folder_path": folder_path,
            "file_name": path.name,
            "code": code,
            "revision": revision,
            "title": title,
        })
    return rows


def write_manifest(rows: list[dict], out: Path) -> None:
    with out.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, fieldnames=MANIFEST_COLUMNS)
        writer.writeheader()
        writer.writerows(rows)


def read_manifest(path: Path) -> list[dict]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))
    missing = [column for column in MANIFEST_COLUMNS if rows and column not in rows[0]]
    if missing:
        raise SystemExit(f"Manifest is missing columns: {', '.join(missing)}")
    for row in rows:
        for column in MANIFEST_COLUMNS:
            row[column] = (row.get(column) or "").strip()
    return rows
