# AGENTS

## What to know
- Use `just` for common tasks: `restore`, `build`, `test`, `format`.
- Before committing, run `dotnet format .\finebytes.slnx` then `dotnet build .\finebytes.slnx`.
- Follow project code style from `.editorconfig`.
- Follow constraints in `.cursor/rules/*.mdc`.

## References
- `CONTRIBUTING.md`
- `docs/mfr-folder-layering.md`
- `docs/magic-file-renamer-design.md` (consult for architecture/feature-level changes)
- `docs/audio-tag-model.md` (embedded audio tag read/write/Apply)
- `docs/image-metadata-model.md` (lazy MetadataExtractor image property cache)
- `.editorconfig`
- `.cursor/rules/refactor-no-legacy-compat.mdc`
- `.cursor/rules/csharp-style.mdc`
