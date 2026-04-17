# AGENTS

## What to know
- Use `just` for common tasks: `restore`, `build`, `test`, `format`.
- Before committing, run `dotnet format .\finebytes.slnx` then `dotnet build .\finebytes.slnx`.
- Follow project code style from `.editorconfig`.
- This project favors forward refactors over legacy compatibility shims.

## References
- `CONTRIBUTING.md`
- `docs/mfr-folder-layering.md`
- `docs/magic-file-renamer-design.md` (consult for architecture/feature-level changes)
- `.editorconfig`
- `.cursor/rules/refactor-no-legacy-compat.mdc`
- `.cursor/rules/csharp-style.mdc`
