# AGENTS

## What to know

- Use `just` for common tasks: `restore`, `build`, `test`, `format`, `lint`, `run-ui`.
- Before committing, run `just format` then `just lint` (or `just build` after format if you only need compile/analyzer checks).
- Markdown tools need a local `.venv` once: `python -m venv .venv` and `pip install -r requirements-md.txt` (see `CONTRIBUTING.md`).
- Formatting: CSharpier for `.cs` / `.csproj` / `.axaml` / `.slnx` layout; `dotnet format style` / `analyzers` for non-whitespace rules; mdformat (+ gfm, frontmatter) for `.md` (via `just format-md`, also part of `just format`).
- Linting: `just lint` runs C# checks (`dotnet build`, `csharpier check`, `dotnet format … --verify-no-changes`) and markdown checks (`just lint-md`: `mdformat --check`, `pymarkdown scan`).
- Follow project code style from `.editorconfig`.
- Follow constraints in `.cursor/rules/*.mdc`.

## References

- `CONTRIBUTING.md`
- `docs/mfr-folder-layering.md`
- `docs/magic-file-renamer-design.md` (consult for architecture/feature-level changes)
- `docs/keyboard-shortcuts.md` (menu/toolbar/File List keys; keep in sync with `AppShortcuts`)
- `docs/audio-tag-model.md` (embedded audio tag read/write/Apply)
- `docs/image-metadata-model.md` (lazy MetadataExtractor image properties + EXIF cache)
- `docs/debts.md` (deferred / later-work list)
- `docs/rename-list-phase5-followups.md` (deferred shuttle/column refactors after Phase 5)
- `.editorconfig`
- `.cursor/rules/refactor-no-legacy-compat.mdc`
- `.cursor/rules/csharp-style.mdc`
