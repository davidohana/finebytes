[private]
default: help

help:
    @just --list

restore:
    dotnet restore

build:
    dotnet build 

clean:
    dotnet build-server shutdown
    - dotnet nuget locals all --clear
    rm -rf **/bin **/obj
    dotnet clean

test:
    dotnet test -c Debug  --logger "console;verbosity=detailed"

format:
    dotnet tool restore
    dotnet csharpier format .
    dotnet format style ./finebytes.slnx
    dotnet format analyzers ./finebytes.slnx
    just format-md

lint:
    just lint-cs
    just lint-md

lint-cs:
    dotnet tool restore
    dotnet build ./finebytes.slnx
    dotnet csharpier check .
    dotnet format style ./finebytes.slnx --verify-no-changes
    dotnet format analyzers ./finebytes.slnx --verify-no-changes

# Tracked + untracked *.md (respects .gitignore); skip .cursor/**
# One-time: python -m venv .venv && .venv/Scripts/pip install -r requirements-md.txt
md_python := if os_family() == "windows" { ".venv/Scripts/python.exe" } else { ".venv/bin/python" }
md_files := `git ls-files -c -o --exclude-standard -- "*.md" ":!.cursor/**" | while IFS= read -r f; do [ -f "$f" ] && printf '%s ' "$f"; done`

format-md:
    {{md_python}} -m mdformat {{md_files}}

lint-md:
    {{md_python}} -m mdformat --check {{md_files}}
    {{md_python}} -m pymarkdown --config pymarkdown.toml scan {{md_files}}

run-help:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- --help

run *args:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- {{args}}

run-ui:
    dotnet run --project ./Mfr.App.Ui/Mfr.App.Ui.csproj

# Help screenshot regeneration (headless Avalonia + optional Pillow hotspots).
# One-time for hotspots: .venv/bin/pip install -r help/tools/requirements.txt
# (or .venv\Scripts\pip install -r help/tools/requirements.txt on Windows).

ensure-help-tools:
    {{md_python}} -c "import PIL" 2>/dev/null || {{md_python}} -m pip install -r help/tools/requirements.txt

annotate-help-hotspots: ensure-help-tools
    {{md_python}} help/tools/annotate-hotspots.py

sync-help-img-dims:
    {{md_python}} help/tools/sync-help-img-dims.py

# Filter option bodies → help/images/{Type}.png + HTML width/height sync.
capture-help-filters:
    #!/usr/bin/env bash
    set -euo pipefail
    export MFR_CAPTURE_HELP_SCREENSHOTS=1
    dotnet test ./Mfr.Tests/Mfr.Tests.csproj --filter FullyQualifiedName~HelpScreenshotCaptureTests
    just sync-help-img-dims

# P0/P1/P2 UI + guide shots → help/images/ui|guide/, then hotspots + HTML dims.
capture-help-ui:
    #!/usr/bin/env bash
    set -euo pipefail
    export MFR_CAPTURE_HELP_SCREENSHOTS=1
    dotnet test ./Mfr.Tests/Mfr.Tests.csproj --filter FullyQualifiedName~HelpUiScreenshotCaptureTests
    just annotate-help-hotspots
    just sync-help-img-dims

# Full help image refresh (filters + UI/guide).
capture-help: capture-help-filters capture-help-ui
