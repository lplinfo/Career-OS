#!/usr/bin/env bash

set -e

echo "🚀 Creating CareerOS documentation structure..."

# =========================
# Directories
# =========================

mkdir -p docs/{vision,architecture,domain,database,api,frontend,ai,security,product,rfcs,adrs,templates}

# =========================
# Root README
# =========================

cat > docs/README.md <<'EOF'
# CareerOS Documentation

Welcome to the official CareerOS documentation.

## Sections

- Vision
- Architecture
- Domain
- Database
- API
- Frontend
- AI
- Security
- Product
- RFCs
- ADRs
- Templates
EOF

# =========================
# Vision
# =========================

touch docs/vision/{product-vision,mission,target-audience,personas,product-principles,glossary}.md

# =========================
# Architecture
# =========================

touch docs/architecture/{architecture-overview,system-context,layers,dependency-rules,conventions,coding-standards,folder-structure,decisions}.md

# =========================
# Domain
# =========================

touch docs/domain/{core-domain,ubiquitous-language,bounded-contexts,aggregates,entities,value-objects,domain-services,domain-events,business-rules}.md

# =========================
# Database
# =========================

touch docs/database/{database-design,erd,naming-conventions,migrations,indexing}.md

# =========================
# API
# =========================

touch docs/api/{api-guidelines,versioning,authentication,errors,pagination,conventions}.md

# =========================
# Frontend
# =========================

touch docs/frontend/{frontend-architecture,ui-guidelines,routing,state-management,forms}.md

# =========================
# AI
# =========================

touch docs/ai/{ai-architecture,interview-engine,prompt-library,providers,guardrails}.md

# =========================
# Security
# =========================

touch docs/security/{authentication,authorization,privacy,gdpr,lgpd}.md

# =========================
# Product
# =========================

touch docs/product/{roadmap,milestones,backlog,releases,user-journeys}.md

# =========================
# RFCs
# =========================

touch docs/rfcs/{RFC-001-foundation,RFC-002-career-profile,RFC-003-resume-engine}.md

# =========================
# ADRs
# =========================

touch docs/adrs/{ADR-001-foundation,ADR-002-layered-architecture,ADR-003-postgresql}.md

# =========================
# Templates
# =========================

touch docs/templates/{RFC-template,ADR-template,PR-template,Architecture-Review-template}.md

# =========================
# Add title to every markdown
# =========================

find docs -name "*.md" | while read file
do
    if [ ! -s "$file" ]; then
        title=$(basename "$file" .md | tr '-' ' ')
        echo "# $title" > "$file"
    fi
done

echo
echo "✅ Documentation structure created successfully!"
echo
tree docs || find docs
