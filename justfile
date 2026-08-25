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

[unix]
format-md:
    npm ci
    npm run format:md

[windows]
format-md:
    cmd.exe //c "scripts\\run-npm.cmd ci && scripts\\run-npm.cmd run format:md"

[unix]
lint-md:
    npm ci
    npm run lint:md

[windows]
lint-md:
    cmd.exe //c "scripts\\run-npm.cmd ci && scripts\\run-npm.cmd run lint:md"

run-help:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- --help

run *args:
    dotnet run --project ./Mfr.App.Cli/Mfr.App.Cli.csproj -- {{args}}

run-ui:
    dotnet run --project ./Mfr.App.Ui/Mfr.App.Ui.csproj
