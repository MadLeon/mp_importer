# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

Extract Manufacturing Process entries from `.xlsm` files in a target folder. Extracted entries are manually reviewed and then stored in a database.

## Development Phases

1. Build project UI
2. Implement business logic with sample data
3. Database script development

## Planned UI

**Main Window**

- **Add Files button** — opens a multi-file dialog; selected `.xlsm` files are sorted by filename and appended to the file list
- **File List area** — one row per file, showing filename (and additional columns TBD)

## Notes

- `.xlsm` file format spec and database schema are TBD — check `project_description.md` and `.claude/skills/database` for the latest details once available
- Tech stack has not yet been decided
