#!/bin/bash

set -e

# Recupera ultimo tag
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")

echo "Last tag: $LAST_TAG"

# Usa git-cliff per suggerire bump
NEXT_VERSION=$(git cliff --bumped-version)

echo "Suggested next version: $NEXT_VERSION"

read -p "Proceed with tag $NEXT_VERSION? (y/n): " confirm

if [ "$confirm" != "y" ]; then
  echo "Aborted."
  exit 1
fi

# Crea tag
git tag $NEXT_VERSION
git push origin $NEXT_VERSION

echo "✅ Release $NEXT_VERSION triggered!"
