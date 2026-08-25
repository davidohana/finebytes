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
