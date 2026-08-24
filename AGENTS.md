# AGENTS

## What to know
- Use `just` for common tasks: `restore`, `build`, `test`, `format`, `run-ui`.
- Before committing, run `just format` then `dotnet build .\finebytes.slnx`.
- Formatting: CSharpier for `.cs` / `.csproj` / `.axaml` / `.slnx` layout; `dotnet format style` / `analyzers` for non-whitespace rules.
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
- `.editorconfig`
- `.cursor/rules/refactor-no-legacy-compat.mdc`
- `.cursor/rules/csharp-style.mdc`
