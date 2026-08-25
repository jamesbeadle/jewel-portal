# Bulk-load drawings (one-off)

Loads a folder tree of drawing files into one project's drawing register without the
upload form: files go straight into the `drawings` blob container under the path the portal
expects (`{projectId}/{drawingId}/{revisionId}/{fileName}`), and a generated, guarded `.sql`
registers the folders, drawings and revisions. Sub-directories become sub-folders on the
register; every file lands as its drawing's **Approved** revision unless `--unapproved` is
given. Files that share a drawing code become one drawing — the highest revision letter is
approved, the rest archived.

Needs Python 3.10+ and `pip install azure-storage-blob`, the storage account's connection
string (the same value the API has as `DrawingsStorage:ConnectionString` / `AzureWebJobsStorage`),
and the `ProjectId` of the target project (from the project's URL in the portal).

## Runbook

```
cd /Users/james/Documents/Claude/Projects/jewel-portal/scripts/bulk-load-drawings
pip3 install azure-storage-blob
python3 bulk_load_drawings.py manifest --source "/path/to/the drawings" --out manifest.csv
open manifest.csv
```

Check `manifest.csv` in Excel: `folder_path` (`Architect/Planning` = sub-folder Planning inside
Architect; blank = Ungrouped), `code`, `revision` and `title` are guessed from each file name and
are all optional — blank whatever is wrong. A file with no title shows its file name on the
register. Save it as CSV, then:

```
export DRAWINGS_STORAGE_CONNECTION_STRING='DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net'
python3 bulk_load_drawings.py load --source "/path/to/the drawings" --manifest manifest.csv --project JBB-2026-001 --approved-by nigel@jewelbb.co.uk --dry-run
python3 bulk_load_drawings.py load --source "/path/to/the drawings" --manifest manifest.csv --project JBB-2026-001 --approved-by nigel@jewelbb.co.uk
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i load-drawings.sql -b -o load-drawings.log
cat load-drawings.log
```

The dry run writes the `.sql` and lists every upload without touching storage — read it
once, then run the real load. Both `load` runs and the `.sql` are safe to repeat: ids are
remembered in `manifest.csv.ids.csv`, blobs already uploaded are skipped, folders are matched
by name, and a drawing code the project already has is skipped with a `PRINT` in the log.
Pass `--issued-by architect@example.com` to record an issuer; without it the issuer is left
blank, as the portal's upload form now allows.
