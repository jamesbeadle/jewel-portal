#!/usr/bin/env python3
"""One-off bulk load of drawings into the JPMS drawing register.

  manifest  walk a folder of files → manifest.csv (edit it in Excel: folder_path, code,
            revision, title are all optional and free to change)
  load      upload every file in the manifest to the `drawings` blob container under the path
            the portal expects ({projectId}/{drawingId}/{revisionId}/{fileName}) and write the
            guarded .sql that registers folders, drawings and revisions — run that with sqlcmd.

Ids are minted once and remembered in <manifest>.ids.csv, so re-running `load` re-uses them:
already-uploaded blobs are skipped and the SQL is idempotent.
"""
import argparse
import csv
import mimetypes
import os
import uuid
from pathlib import Path

import drawing_manifest as manifest
import drawing_sql as sql

CONTAINER_NAME = "drawings"
CONNECTION_STRING_VARIABLE = "DRAWINGS_STORAGE_CONNECTION_STRING"
DEFAULT_CONTENT_TYPE = "application/octet-stream"
IDS_SUFFIX = ".ids.csv"


def new_id() -> str:
    return uuid.uuid4().hex  # the API's compact-GUID identifiers


def folder_id_for(project_id: str, folder_path: str) -> str:
    """Stable per project + path, so a re-run renders the same .sql (the insert is guarded anyway)."""
    return uuid.uuid5(uuid.NAMESPACE_URL, f"jpms-drawing-folder/{project_id}/{folder_path}").hex


def blob_ref(project_id: str, drawing_id: str, revision_id: str, file_name: str) -> str:
    return f"{project_id}/{drawing_id}/{revision_id}/{file_name}"


def content_type_of(file_name: str) -> str:
    return mimetypes.guess_type(file_name)[0] or DEFAULT_CONTENT_TYPE


def load_ids(path: Path) -> dict[str, dict]:
    if not path.exists():
        return {}
    with path.open(newline="", encoding="utf-8") as handle:
        return {row["relative_path"]: row for row in csv.DictReader(handle)}


def save_ids(path: Path, ids: dict[str, dict]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=["relative_path", "drawing_id", "revision_id"])
        writer.writeheader()
        writer.writerows(ids.values())


def plan_folders(rows: list[dict], project_id: str) -> dict[str, sql.FolderPlan]:
    """Every folder path in the manifest, parents before children, each with a T-SQL variable."""
    plans: dict[str, sql.FolderPlan] = {}
    for row in rows:
        parts = [part for part in row["folder_path"].split(manifest.FOLDER_SEPARATOR) if part.strip()]
        for depth in range(len(parts)):
            path = manifest.FOLDER_SEPARATOR.join(parts[: depth + 1])
            if path in plans:
                continue
            parent = manifest.FOLDER_SEPARATOR.join(parts[:depth]) if depth else None
            plans[path] = sql.FolderPlan(
                variable=f"@folder{len(plans) + 1}", name=parts[depth].strip(),
                parent_variable=plans[parent].variable if parent else None, new_id=folder_id_for(project_id, path))
    return plans


def plan_drawings(rows: list[dict], ids: dict[str, dict], folders: dict[str, sql.FolderPlan],
                  project_id: str, source: Path, approve: bool) -> list[sql.DrawingPlan]:
    """Rows sharing a code become one drawing; the highest revision is approved, the rest archived."""
    by_key: dict[str, sql.DrawingPlan] = {}
    for row in rows:
        remembered = ids.get(row["relative_path"])
        key = row["code"].lower() if row["code"] else row["relative_path"]
        drawing = by_key.get(key)
        if drawing is None:
            drawing_id = remembered["drawing_id"] if remembered else new_id()
            folder = folders.get(row["folder_path"]) if row["folder_path"] else None
            drawing = sql.DrawingPlan(drawing_id, row["code"], row["title"], folder.variable if folder else None)
            by_key[key] = drawing
        revision_id = remembered["revision_id"] if remembered else new_id()
        ids[row["relative_path"]] = {"relative_path": row["relative_path"], "drawing_id": drawing.drawing_id, "revision_id": revision_id}
        drawing.revisions.append(sql.RevisionPlan(
            revision_id, row["revision"].upper(), row["file_name"],
            blob_ref(project_id, drawing.drawing_id, revision_id, row["file_name"]),
            content_type_of(row["file_name"]), (source / row["relative_path"]).stat().st_size,
            sql.UNAPPROVED))
    for drawing in by_key.values():
        mark_statuses(drawing, approve)
    return list(by_key.values())


def mark_statuses(drawing: sql.DrawingPlan, approve: bool) -> None:
    if not approve:
        return
    latest = max(drawing.revisions, key=lambda revision: (len(revision.label), revision.label))
    for revision in drawing.revisions:
        revision.status = sql.APPROVED if revision is latest else sql.ARCHIVED


def open_container(connection_string: str):
    from azure.storage.blob import BlobServiceClient
    container = BlobServiceClient.from_connection_string(connection_string).get_container_client(CONTAINER_NAME)
    if not container.exists():
        container.create_container()
    return container


def upload_blobs(source: Path, rows: list[dict], ids: dict[str, dict], project_id: str, dry_run: bool) -> None:
    connection_string = os.environ.get(CONNECTION_STRING_VARIABLE)
    if not connection_string and not dry_run:
        raise SystemExit(f"Set {CONNECTION_STRING_VARIABLE} to the storage account's connection string.")
    container = None if dry_run else open_container(connection_string)
    for index, row in enumerate(rows, start=1):
        remembered = ids[row["relative_path"]]
        ref = blob_ref(project_id, remembered["drawing_id"], remembered["revision_id"], row["file_name"])
        if dry_run:
            print(f"[{index}/{len(rows)}] would upload {row['relative_path']} → {ref}")
            continue
        blob = container.get_blob_client(ref)
        if blob.exists():
            print(f"[{index}/{len(rows)}] already uploaded {ref}")
            continue
        from azure.storage.blob import ContentSettings
        with (source / row["relative_path"]).open("rb") as handle:
            blob.upload_blob(handle, content_settings=ContentSettings(content_type=content_type_of(row["file_name"])))
        print(f"[{index}/{len(rows)}] uploaded {ref}")


def run_manifest(args: argparse.Namespace) -> None:
    rows = manifest.build_rows(Path(args.source))
    manifest.write_manifest(rows, Path(args.out))
    print(f"{len(rows)} files → {args.out}. Check folder_path / code / revision / title, then run `load`.")


def run_load(args: argparse.Namespace) -> None:
    source, manifest_path = Path(args.source), Path(args.manifest)
    rows = manifest.read_manifest(manifest_path)
    ids_path = manifest_path.with_name(manifest_path.name + IDS_SUFFIX)
    ids = load_ids(ids_path)
    folders = plan_folders(rows, args.project)
    drawings = plan_drawings(rows, ids, folders, args.project, source, approve=not args.unapproved)
    save_ids(ids_path, ids)
    upload_blobs(source, rows, ids, args.project, args.dry_run)
    Path(args.out).write_text(sql.render(args.project, list(folders.values()), drawings, args.issued_by, args.approved_by), encoding="utf-8")
    print(f"{len(folders)} folders, {len(drawings)} drawings, {len(rows)} revisions → {args.out}"
          + (" (dry run — nothing uploaded)" if args.dry_run else ""))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    make = commands.add_parser("manifest", help="walk a folder and write manifest.csv")
    make.add_argument("--source", required=True, help="folder holding the drawing files (sub-folders become sub-folders)")
    make.add_argument("--out", default="manifest.csv")
    make.set_defaults(run=run_manifest)
    load = commands.add_parser("load", help="upload the files and write the registration .sql")
    load.add_argument("--source", required=True)
    load.add_argument("--manifest", default="manifest.csv")
    load.add_argument("--project", required=True, help="the ProjectId the drawings belong to")
    load.add_argument("--approved-by", required=True, help="email recorded as the approver")
    load.add_argument("--issued-by", default="", help="email recorded as the issuer (blank = not recorded)")
    load.add_argument("--unapproved", action="store_true", help="land every file Unapproved instead of Approved")
    load.add_argument("--dry-run", action="store_true", help="write the .sql and list the uploads without touching storage")
    load.add_argument("--out", default="load-drawings.sql")
    load.set_defaults(run=run_load)
    args = parser.parse_args()
    args.run(args)


if __name__ == "__main__":
    main()
