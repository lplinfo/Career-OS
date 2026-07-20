#!/usr/bin/env bash

set -e

echo "📚 Creating CareerOS Domain Documentation..."

BASE="docs/domain"

mkdir -p "$BASE"

FILES=(
README
core-domain
ubiquitous-language
bounded-contexts
aggregates
entities
value-objects
domain-services
domain-events
business-rules
workflows
relationships
lifecycle
validation-rules
future-ideas
)

for file in "${FILES[@]}"
do
    touch "$BASE/$file.md"

    if [ ! -s "$BASE/$file.md" ]; then
        TITLE=$(echo "$file" | tr '-' ' ')
        TITLE="$(tr '[:lower:]' '[:upper:]' <<< ${TITLE:0:1})${TITLE:1}"

        cat > "$BASE/$file.md" <<EOF
# $TITLE

## Purpose

> TODO

---

## Description

TODO

---

## Notes

TODO
EOF
    fi
done

echo
echo "✅ Domain documentation created!"
echo

find "$BASE" -type f | sort
