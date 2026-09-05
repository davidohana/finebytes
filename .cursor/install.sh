#!/usr/bin/env bash
# Repository bootstrap for the Cloud Agent environment.
# Runs after the source tree is checked out. Must be idempotent.
set -euo pipefail

cd "$(git rev-parse --show-toplevel 2>/dev/null || echo /workspace)"

# Restore local dotnet tools (CSharpier + dotnet-format used by `just format`/`just lint`).
dotnet tool restore

# Restore and build the whole solution.
dotnet restore ./finebytes.slnx
dotnet build ./finebytes.slnx -c Debug

# Python venv for the markdown format/lint tooling (mdformat + pymarkdown).
if [ ! -x .venv/bin/python ]; then
    python3 -m venv .venv
fi
.venv/bin/pip install --upgrade pip >/dev/null
.venv/bin/pip install -r requirements-md.txt
