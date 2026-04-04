default:
    @just --list

restore:
    dotnet restore ./mfr8/mfr8.csproj
    dotnet restore ./mfr8.Tests/mfr8.Tests.csproj

build:
    dotnet build ./finebytes.slnx

test:
    dotnet test ./mfr8.Tests/mfr8.Tests.csproj -c Debug

format:
    dotnet format ./finebytes.slnx

run-help:
    dotnet run --project ./mfr8/mfr8.csproj -- --help
