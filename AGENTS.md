# GameEngine — Agent Instructions

## Project Documentation & Standards

Before writing or changing any code — even for quick, direct requests that don't go through a `/*` workflow — ground yourself in the project's documentation:

1. Read @.maister/docs/INDEX.md to see what's documented. It is the map to everything the team maintains — coding standards by domain, project vision/tech-stack/architecture, and any other project knowledge (business domain, glossaries, decisions, etc.).
2. Then open and read the specific files it points to that are relevant to your task — standards AND any project/domain docs. The index alone is not enough.
3. Follow the standards as you work (they represent team decisions; if one conflicts with the task, ask the user) and use the project docs as context.

### Standards Evolution

When you notice recurring patterns, fixes, or conventions during implementation that aren't yet captured in standards — suggest adding them. Examples:
- A bug fix reveals a pattern that should be standardized (e.g., "always validate X before Y")
- PR review feedback identifies a convention the team wants enforced
- The same type of fix is needed across multiple files
- A new library/pattern is adopted that should be documented

When this happens, briefly suggest the standard to the user. If approved, invoke `/standards-update` with the identified pattern.

## Maister Workflows

This project uses the maister plugin for structured development workflows. When any `/*` command is invoked, execute it by reading and following that skill's `SKILL.md` immediately — do not skip workflows for "straightforward" tasks. The user chose the workflow intentionally; complexity assessment is the workflow's job.
