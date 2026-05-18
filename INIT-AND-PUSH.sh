#!/usr/bin/env bash
# One-shot script: cleans up the half-initialised git folder created by the sandbox,
# re-inits the repo, makes the initial commit, and pushes to GitHub.
#
# Usage (from a normal Terminal — NOT inside Claude's shell):
#   cd "/Users/james/Documents/Claude/Projects/Jewel-Enterprises"
#   chmod +x INIT-AND-PUSH.sh
#   ./INIT-AND-PUSH.sh
#
# After this runs successfully you can delete this file:
#   rm INIT-AND-PUSH.sh

set -euo pipefail

REPO_URL="https://github.com/jamesbeadle/jewel-enterprises.git"
BRANCH="main"

echo "==> Cleaning any existing .git folder…"
rm -rf .git

echo "==> Initialising repo on branch '$BRANCH'…"
git init -b "$BRANCH"

echo "==> Staging all files…"
git add -A

echo "==> Creating initial commit…"
git commit -m "Initial scoping repository scaffold

Adds the discovery-phase folder structure, conventions, and worked examples
that drive the on-site role-play scoping approach.

- Root README: living scoping dashboard with status tracking
- /docs/user-journeys: template + first worked journey (sales lead -> won deal)
- /docs/ui-components: atomic-design layout + button worked example
- /docs/workflows: Mermaid cross-actor flow example
- /docs/data-models: lead JSON Schema + entity-relationship sketch
- /docs/requirements: personas template + permission matrix
- /docs/meetings: kickoff note template
- /prototypes, /assets: scaffolded for later Blazor PWA + media
- .gitignore for .NET/Node/macOS/IDE artefacts"

echo "==> Setting remote 'origin' to $REPO_URL…"
git remote add origin "$REPO_URL" || git remote set-url origin "$REPO_URL"

echo "==> Pushing to origin/$BRANCH…"
# If the GitHub repo was created with an initial commit (README/.gitignore),
# this push will be rejected. In that case, run:
#     git pull --rebase origin main && git push -u origin main
# OR, if you're certain the remote is empty/safe to overwrite:
#     git push -u --force origin main
git push -u origin "$BRANCH"

echo
echo "✅ Done. View it at: https://github.com/jamesbeadle/jewel-enterprises"
