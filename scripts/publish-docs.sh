#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOCS_DIR="${DOCS_DIR:-$ROOT_DIR/../ClothPhysicsVerletIntergrationCSNEA-docs}"
COMMIT_MSG="${1:-Update generated Doxygen docs}"

if [[ ! -f "$ROOT_DIR/Doxyfile" ]]; then
  echo "Error: Doxyfile not found at $ROOT_DIR/Doxyfile" >&2
  exit 1
fi

if [[ ! -d "$DOCS_DIR" ]]; then
  echo "Error: docs worktree directory does not exist: $DOCS_DIR" >&2
  echo "Create it with: git worktree add ../ClothPhysicsVerletIntergrationCSNEA-docs gh-pages" >&2
  exit 1
fi

if [[ ! -d "$DOCS_DIR/.git" && ! -f "$DOCS_DIR/.git" ]]; then
  echo "Error: docs directory is not a git worktree: $DOCS_DIR" >&2
  exit 1
fi

echo "Generating docs from $ROOT_DIR/Doxyfile ..."
cd "$ROOT_DIR"
doxygen Doxyfile

echo "Staging docs in $DOCS_DIR ..."
cd "$DOCS_DIR"
git add .

if git diff --cached --quiet; then
  echo "No documentation changes to commit."
  exit 0
fi

echo "Committing docs ..."
git commit -m "$COMMIT_MSG"

echo "Pushing docs branch ..."
git push

echo "Done. Docs updated and pushed from $DOCS_DIR"
