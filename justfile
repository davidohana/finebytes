default:
    @just --list

restore:
    dotnet restore ./Mfr/Mfr.csproj
    dotnet restore ./Mfr.Tests/Mfr.Tests.csproj

build:
    dotnet build ./finebytes.slnx

clean:
    dotnet clean ./finebytes.slnx

test:
    dotnet test ./Mfr.Tests/Mfr.Tests.csproj -c Debug  --logger "console;verbosity=detailed"

format:
    dotnet format ./finebytes.slnx --verbosity diagnostic

run-help:
    dotnet run --project ./Mfr/Mfr.csproj -- --help

run *args:
    dotnet run --project ./Mfr/Mfr.csproj -- {{args}}
